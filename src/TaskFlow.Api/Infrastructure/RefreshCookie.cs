using Microsoft.Extensions.Options;
using TaskFlow.Api.Options;

namespace TaskFlow.Api.Infrastructure;

/// <summary>
/// Reads and writes the refresh-token cookie: httpOnly (scripts can't read it), SameSite=Strict
/// (never sent cross-site) and scoped to /api/auth (never sent with ordinary API calls).
/// </summary>
public sealed class RefreshCookie
{
    public const string Name = "tf_refresh";
    private const string Path = "/api/auth";
    private readonly IOptionsMonitor<AuthOptions> _options;

    public RefreshCookie(IOptionsMonitor<AuthOptions> options) => _options = options;

    public string? Read(HttpRequest request) => request.Cookies[Name];

    public void Set(HttpResponse response, string token, DateTime expiresAt) =>
        response.Cookies.Append(Name, token, Options(expiresAt));

    public void Clear(HttpResponse response) =>
        response.Cookies.Delete(Name, Options(DateTime.UnixEpoch));

    private CookieOptions Options(DateTime expires) => new()
    {
        HttpOnly = true,
        Secure = _options.CurrentValue.CookieSecure,
        SameSite = SameSiteMode.Strict,
        Path = Path,
        Expires = expires,
        IsEssential = true
    };
}
