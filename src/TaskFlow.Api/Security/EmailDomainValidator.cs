using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using TaskFlow.Api.Domain;
using TaskFlow.Api.Options;

namespace TaskFlow.Api.Security;

/// <summary>Only lets accounts be created for the configured company email domains.</summary>
public sealed class EmailDomainValidator : IUserValidator<AppUser>
{
    public const string ErrorCode = "EmailDomainNotAllowed";
    private readonly IOptionsMonitor<AuthOptions> _options;

    public EmailDomainValidator(IOptionsMonitor<AuthOptions> options) => _options = options;

    public Task<IdentityResult> ValidateAsync(UserManager<AppUser> manager, AppUser user)
    {
        var allowed = _options.CurrentValue.AllowedEmailDomains;
        if (allowed.Length == 0 || IsAllowed(user.Email, allowed))
        {
            return Task.FromResult(IdentityResult.Success);
        }

        return Task.FromResult(IdentityResult.Failed(new IdentityError
        {
            Code = ErrorCode,
            Description = "Registration is limited to company email addresses."
        }));
    }

    public static bool IsAllowed(string? email, IEnumerable<string> allowedDomains)
    {
        var at = email?.LastIndexOf('@') ?? -1;
        if (at < 0)
        {
            return false;
        }

        var domain = email![(at + 1)..];
        return allowedDomains.Any(d => string.Equals(d.TrimStart('@'), domain, StringComparison.OrdinalIgnoreCase));
    }
}
