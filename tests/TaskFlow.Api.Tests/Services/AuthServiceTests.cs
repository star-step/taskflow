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

    private Task<InvalidCredentialsException> FailedLogin(string email, string password) =>
        Assert.ThrowsAsync<InvalidCredentialsException>(() =>
            WithAuth(a => a.LoginAsync(new LoginRequest { Email = email, Password = password })));

    [Fact]
    public async Task LoginAsync_EveryFailure_LooksIdentical()
    {
        // An unknown email, each wrong password, and a locked account must be indistinguishable,
        // or the response reveals which emails have accounts.
        await Register();
        var unknown = await FailedLogin("nobody@company.com", Password);

        var failures = new List<InvalidCredentialsException>();
        for (var i = 0; i < 5; i++)
        {
            failures.Add(await FailedLogin("jordan.ellis@company.com", "not-the-password"));
        }

        failures.Add(await FailedLogin("jordan.ellis@company.com", Password)); // locked now, right password

        Assert.All(failures, f =>
        {
            Assert.Equal(unknown.ErrorCode, f.ErrorCode);
            Assert.Equal(unknown.Message, f.Message);
            Assert.Equal(unknown.StatusCode, f.StatusCode);
        });
        Assert.Equal(ErrorCodes.InvalidCredentials, unknown.ErrorCode);
        Assert.DoesNotMatch(@"\d+\s+attempts?\s+left", unknown.Message); // no per-account countdown
    }

    [Fact]
    public async Task LoginAsync_FailureMessage_UsesConfiguredLockoutDuration()
    {
        await using var host = new TestHost(new() { ["Auth:Lockout:Minutes"] = "30" });
        await using var scope = host.Scope();
        var auth = scope.ServiceProvider.GetRequiredService<IAuthService>();

        var ex = await Assert.ThrowsAsync<InvalidCredentialsException>(() =>
            auth.LoginAsync(new LoginRequest { Email = "nobody@company.com", Password = Password }));

        Assert.Contains("30 minutes", ex.Message);
    }

    [Fact]
    public async Task LoginAsync_FiveWrongPasswords_LocksAccount()
    {
        await Register();

        for (var i = 0; i < 5; i++)
        {
            await FailedLogin("jordan.ellis@company.com", "not-the-password");
        }

        // Locked: even the right password is refused, and the account really is locked.
        await FailedLogin("jordan.ellis@company.com", Password);
        await using var scope = _host.Scope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        Assert.True(await users.IsLockedOutAsync((await users.FindByEmailAsync("jordan.ellis@company.com"))!));
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
    public async Task RefreshAsync_ReuseExactlyAtGraceLimit_IsAllowed_ButNotAfter()
    {
        var first = await Register();
        await WithAuth(a => a.RefreshAsync(first.RefreshToken));

        _host.Clock.Advance(TimeSpan.FromSeconds(20));
        await WithAuth(a => a.RefreshAsync(first.RefreshToken)); // boundary: still the benign two-tab race

        _host.Clock.Advance(TimeSpan.FromMilliseconds(1));
        await Assert.ThrowsAsync<InvalidRefreshTokenException>(() => WithAuth(a => a.RefreshAsync(first.RefreshToken)));
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
