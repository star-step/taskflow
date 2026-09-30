using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TaskFlow.Api.Data;
using TaskFlow.Api.Domain;
using TaskFlow.Api.Errors;
using TaskFlow.Api.Options;
using TaskFlow.Api.Security;
using TaskFlow.Shared.Contracts.Auth;

namespace TaskFlow.Api.Services;

public sealed class AuthService : IAuthService
{
    /// <summary>
    /// Two tabs can refresh with the same cookie at almost the same moment. A token that was rotated
    /// this recently is treated as that race, not as theft, and gets a fresh token in the same family.
    /// </summary>
    internal static readonly TimeSpan ReuseGrace = TimeSpan.FromSeconds(20);

    private static readonly AppUser DummyUser = new();
    private static string? _dummyHash;

    private readonly UserManager<AppUser> _users;
    private readonly IPasswordHasher<AppUser> _hasher;
    private readonly AppDbContext _db;
    private readonly ITokenService _tokens;
    private readonly AuthOptions _options;

    public AuthService(UserManager<AppUser> users, IPasswordHasher<AppUser> hasher, AppDbContext db,
        ITokenService tokens, IOptions<AuthOptions> options)
    {
        _users = users;
        _hasher = hasher;
        _db = db;
        _tokens = tokens;
        _options = options.Value;
    }

    public async Task<AuthResult> RegisterAsync(RegisterRequest request, CancellationToken ct = default)
    {
        var email = request.Email.Trim();
        if (_options.AllowedEmailDomains.Length > 0 && !EmailDomainValidator.IsAllowed(email, _options.AllowedEmailDomains))
        {
            throw new EmailDomainNotAllowedException(_options.AllowedEmailDomains);
        }

        if (await _users.FindByEmailAsync(email) is not null)
        {
            throw new EmailAlreadyRegisteredException();
        }

        var user = new AppUser
        {
            UserName = email,
            Email = email,
            DisplayName = request.DisplayName.Trim(),
            CreatedDate = _db.UtcNow(),
            LockoutEnabled = true
        };

        var result = await _users.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            throw ToException(result.Errors);
        }

        return await IssueAsync(user, Guid.NewGuid(), onIssued: null, ct);
    }

    public async Task<AuthResult> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        // Every failure path below does one password-hash verification and throws the same exception,
        // so neither the response nor its timing reveals whether the email exists or is locked.
        var user = await _users.FindByEmailAsync(request.Email.Trim());
        if (user is null)
        {
            _dummyHash ??= _hasher.HashPassword(DummyUser, Guid.NewGuid().ToString());
            _hasher.VerifyHashedPassword(DummyUser, _dummyHash, request.Password);
            throw new InvalidCredentialsException();
        }

        if (await _users.IsLockedOutAsync(user))
        {
            // Refused without counting another failure, but still pay for a hash check.
            _hasher.VerifyHashedPassword(user, user.PasswordHash ?? string.Empty, request.Password);
            throw new InvalidCredentialsException();
        }

        if (!await _users.CheckPasswordAsync(user, request.Password))
        {
            await _users.AccessFailedAsync(user);
            throw new InvalidCredentialsException();
        }

        await _users.ResetAccessFailedCountAsync(user);
        return await IssueAsync(user, Guid.NewGuid(), onIssued: null, ct);
    }

    public async Task<AuthResult> RefreshAsync(string? refreshToken, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(refreshToken))
        {
            throw new InvalidRefreshTokenException();
        }

        var hash = TokenHasher.Hash(refreshToken);
        var token = await _db.RefreshTokens.Include(t => t.User).FirstOrDefaultAsync(t => t.TokenHash == hash, ct);
        if (token?.User is null)
        {
            throw new InvalidRefreshTokenException();
        }

        var now = _db.UtcNow();
        if (token.RevokedAt is { } revokedAt
            && (token.ReplacedByTokenHash is null || now - revokedAt > ReuseGrace))
        {
            // A revoked token came back: it was stolen and replayed, or the session was signed out.
            // End every session descended from the same sign-in.
            await RevokeFamilyAsync(token.FamilyId, now, ct);
            throw new InvalidRefreshTokenException();
        }

        if (token.ExpiresAt <= now || token.SecurityStamp != token.User.SecurityStamp)
        {
            token.RevokedAt ??= now;
            await _db.SaveChangesAsync(ct);
            throw new InvalidRefreshTokenException();
        }

        return await IssueAsync(token.User, token.FamilyId, newHash =>
        {
            if (token.RevokedAt is null)
            {
                token.RevokedAt = now;
                token.ReplacedByTokenHash = newHash;
            }
        }, ct);
    }

    public async Task LogoutAsync(string? refreshToken, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(refreshToken))
        {
            return;
        }

        var hash = TokenHasher.Hash(refreshToken);
        var familyId = await _db.RefreshTokens
            .Where(t => t.TokenHash == hash)
            .Select(t => (Guid?)t.FamilyId)
            .FirstOrDefaultAsync(ct);
        if (familyId is not null)
        {
            await RevokeFamilyAsync(familyId.Value, _db.UtcNow(), ct);
        }
    }

    public async Task<UserResponse> GetUserAsync(int userId, CancellationToken ct = default)
    {
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId, ct)
            ?? throw new InvalidRefreshTokenException();
        return ToUserResponse(user);
    }

    private async Task<AuthResult> IssueAsync(AppUser user, Guid familyId, Action<string>? onIssued, CancellationToken ct)
    {
        var (accessToken, accessExpires) = _tokens.CreateAccessToken(user);
        var raw = TokenHasher.NewToken();
        var hash = TokenHasher.Hash(raw);
        var now = _db.UtcNow();
        var refreshExpires = now.AddDays(_options.RefreshTokenDays);

        _db.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            TokenHash = hash,
            FamilyId = familyId,
            SecurityStamp = user.SecurityStamp ?? string.Empty,
            CreatedAt = now,
            ExpiresAt = refreshExpires
        });
        onIssued?.Invoke(hash);
        await _db.SaveChangesAsync(ct);

        var response = new AuthResponse { AccessToken = accessToken, ExpiresAt = accessExpires, User = ToUserResponse(user) };
        return new AuthResult(response, raw, refreshExpires);
    }

    private async Task RevokeFamilyAsync(Guid familyId, DateTime now, CancellationToken ct)
    {
        var active = await _db.RefreshTokens.Where(t => t.FamilyId == familyId && t.RevokedAt == null).ToListAsync(ct);
        foreach (var t in active)
        {
            t.RevokedAt = now;
        }

        await _db.SaveChangesAsync(ct);
    }

    private static UserResponse ToUserResponse(AppUser user) => new()
    {
        Id = user.Id,
        Email = user.Email ?? string.Empty,
        DisplayName = user.DisplayName
    };

    private Exception ToException(IEnumerable<IdentityError> identityErrors)
    {
        var errors = identityErrors.ToList();
        if (errors.Any(e => e.Code is nameof(IdentityErrorDescriber.DuplicateEmail) or nameof(IdentityErrorDescriber.DuplicateUserName)))
        {
            return new EmailAlreadyRegisteredException();
        }

        if (errors.Any(e => e.Code == EmailDomainValidator.ErrorCode))
        {
            return new EmailDomainNotAllowedException(_options.AllowedEmailDomains);
        }

        var byField = errors
            .GroupBy(e => e.Code.StartsWith("Password", StringComparison.Ordinal) ? "password"
                : e.Code.Contains("Email", StringComparison.Ordinal) || e.Code.Contains("UserName", StringComparison.Ordinal) ? "email"
                : "request")
            .ToDictionary(g => g.Key, g => g.Select(e => e.Description).Distinct().ToArray());
        return new DomainValidationException(byField);
    }
}
