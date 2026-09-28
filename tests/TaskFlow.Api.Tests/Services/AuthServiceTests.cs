using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TaskFlow.Api.Data;
using TaskFlow.Api.Domain;
using TaskFlow.Api.Errors;
using TaskFlow.Api.Services;
using TaskFlow.Api.Tests.Support;
using TaskFlow.Shared.Contracts.Auth;

namespace TaskFlow.Api.Tests.Services;

public sealed class AuthServiceTests : IAsyncLifetime
{
    private const string Password = "harbour-lantern-47";

    private readonly TestHost _host = new(new() { ["Auth:AllowedEmailDomains:0"] = "company.com" });

    public Task InitializeAsync() => Task.CompletedTask;
    public async Task DisposeAsync() => await _host.DisposeAsync();

    private async Task<T> WithAuth<T>(Func<IAuthService, Task<T>> action)
    {
        await using var scope = _host.Scope();
        return await action(scope.ServiceProvider.GetRequiredService<IAuthService>());
    }

    private Task<AuthResult> Register(string email = "jordan.ellis@company.com", string password = Password) =>
        WithAuth(a => a.RegisterAsync(new RegisterRequest { DisplayName = "Jordan Ellis", Email = email, Password = password }));

    [Fact]
    public async Task RegisterAsync_ReturnsTokens_AndUserCanSignIn()
    {
        var registered = await Register();

        Assert.False(string.IsNullOrEmpty(registered.Response.AccessToken));
        Assert.False(string.IsNullOrEmpty(registered.RefreshToken));
        Assert.Equal("Jordan Ellis", registered.Response.User.DisplayName);

        var login = await WithAuth(a => a.LoginAsync(new LoginRequest { Email = "Jordan.Ellis@company.com", Password = Password }));
        Assert.Equal(registered.Response.User.Id, login.Response.User.Id);
    }

    [Fact]
    public async Task RegisterAsync_DuplicateEmailDifferentCase_ThrowsEmailAlreadyRegistered()
    {
        await Register();

        await Assert.ThrowsAsync<EmailAlreadyRegisteredException>(() => Register("JORDAN.ELLIS@company.com"));
    }

    [Fact]
    public async Task RegisterAsync_DisallowedDomain_ThrowsEmailDomainNotAllowed()
    {
        var ex = await Assert.ThrowsAsync<EmailDomainNotAllowedException>(() => Register("jordan@gmail.com"));

        Assert.Equal(new[] { "Use your @company.com email." }, ex.Errors!["email"]);
    }

    [Theory]
    [InlineData("jordanellis-rocks", "Password can't include your name or email.")]
    [InlineData("password1234", "This password is too easy to guess. Try a few unrelated words.")]
    [InlineData("abcabcabcabc", "This password is too easy to guess. Try a few unrelated words.")]
    public async Task RegisterAsync_WeakPassword_ReturnsPasswordError(string password, string message)
    {
        var ex = await Assert.ThrowsAsync<DomainValidationException>(() => Register(password: password));

        Assert.Contains(message, ex.Errors!["password"]);
    }

    [Fact]
    public async Task LoginAsync_UnknownEmailAndWrongPassword_ShareOneMessage()
    {
        await Register();

        var unknown = await Assert.ThrowsAsync<InvalidCredentialsException>(() =>
            WithAuth(a => a.LoginAsync(new LoginRequest { Email = "nobody@company.com", Password = Password })));
        var wrong = await Assert.ThrowsAsync<InvalidCredentialsException>(() =>
            WithAuth(a => a.LoginAsync(new LoginRequest { Email = "jordan.ellis@company.com", Password = "not-the-password" })));

        Assert.Equal(ErrorCodes.InvalidCredentials, unknown.ErrorCode);
        Assert.Equal(ErrorCodes.InvalidCredentials, wrong.ErrorCode);
        Assert.StartsWith("Email or password is incorrect.", wrong.Message);
    }

    [Fact]
    public async Task LoginAsync_FiveWrongPasswords_LocksAccount()
    {
        await Register();
        var bad = new LoginRequest { Email = "jordan.ellis@company.com", Password = "not-the-password" };

        for (var i = 0; i < 4; i++)
        {
            var ex = await Assert.ThrowsAsync<InvalidCredentialsException>(() => WithAuth(a => a.LoginAsync(bad)));
            Assert.Equal(ErrorCodes.InvalidCredentials, ex.ErrorCode);
        }

        var fifth = await Assert.ThrowsAsync<InvalidCredentialsException>(() => WithAuth(a => a.LoginAsync(bad)));
        Assert.Equal(ErrorCodes.LockedOut, fifth.ErrorCode);

        // Even the right password is refused while locked.
        var locked = await Assert.ThrowsAsync<InvalidCredentialsException>(() =>
            WithAuth(a => a.LoginAsync(new LoginRequest { Email = bad.Email, Password = Password })));
        Assert.Equal(ErrorCodes.LockedOut, locked.ErrorCode);
    }

    [Fact]
    public async Task RefreshAsync_RotatesToken_AndRevokesOld()
    {
        var first = await Register();

        var second = await WithAuth(a => a.RefreshAsync(first.RefreshToken));

        Assert.NotEqual(first.RefreshToken, second.RefreshToken);
        await using var scope = _host.Scope();
        var tokens = await scope.ServiceProvider.GetRequiredService<AppDbContext>().RefreshTokens
            .OrderBy(t => t.Id).ToListAsync();
        Assert.Equal(2, tokens.Count);
        Assert.NotNull(tokens[0].RevokedAt);
        Assert.Equal(tokens[1].TokenHash, tokens[0].ReplacedByTokenHash);
        Assert.Null(tokens[1].RevokedAt);
        Assert.Equal(tokens[0].FamilyId, tokens[1].FamilyId);
    }

    [Fact]
    public async Task RefreshAsync_ReusedRevokedToken_RevokesWholeFamily()
    {
        var first = await Register();
        var second = await WithAuth(a => a.RefreshAsync(first.RefreshToken));

        // After the grace window, replaying the old token looks like theft.
        _host.Clock.Advance(TimeSpan.FromMinutes(1));
        await Assert.ThrowsAsync<InvalidRefreshTokenException>(() => WithAuth(a => a.RefreshAsync(first.RefreshToken)));

        // The legitimate newer token is now dead too.
        await Assert.ThrowsAsync<InvalidRefreshTokenException>(() => WithAuth(a => a.RefreshAsync(second.RefreshToken)));
    }

    [Fact]
    public async Task RefreshAsync_ConcurrentReuseWithinGrace_IsAllowed()
    {
        var first = await Register();
        await WithAuth(a => a.RefreshAsync(first.RefreshToken));

        _host.Clock.Advance(TimeSpan.FromSeconds(5));
        var again = await WithAuth(a => a.RefreshAsync(first.RefreshToken));

        Assert.False(string.IsNullOrEmpty(again.Response.AccessToken));
    }

    [Fact]
    public async Task RefreshAsync_AfterExpiry_Fails()
    {
        var first = await Register();

        _host.Clock.Advance(TimeSpan.FromDays(8));

        await Assert.ThrowsAsync<InvalidRefreshTokenException>(() => WithAuth(a => a.RefreshAsync(first.RefreshToken)));
    }

    [Fact]
    public async Task RefreshAsync_AfterSecurityStampChange_Fails()
    {
        var first = await Register();

        await using (var scope = _host.Scope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
            var user = await users.FindByEmailAsync("jordan.ellis@company.com");
            await users.UpdateSecurityStampAsync(user!);
        }

        await Assert.ThrowsAsync<InvalidRefreshTokenException>(() => WithAuth(a => a.RefreshAsync(first.RefreshToken)));
    }

    [Fact]
    public async Task LogoutAsync_RevokesSession()
    {
        var first = await Register();

        await WithAuth(async a => { await a.LogoutAsync(first.RefreshToken); return true; });

        await Assert.ThrowsAsync<InvalidRefreshTokenException>(() => WithAuth(a => a.RefreshAsync(first.RefreshToken)));
    }
}
