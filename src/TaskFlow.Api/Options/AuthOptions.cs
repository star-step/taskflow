using System.ComponentModel.DataAnnotations;

namespace TaskFlow.Api.Options;

public sealed class AuthOptions
{
    public const string Section = "Auth";

    [Range(1, 90)] public int RefreshTokenDays { get; set; } = 7;

    /// <summary>Email domains allowed to register, e.g. "company.com". Empty means any domain.</summary>
    public string[] AllowedEmailDomains { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Send the refresh cookie with the Secure flag. Keep true anywhere served over HTTPS.
    /// Safari refuses Secure cookies on http://localhost, so local setups may turn this off.
    /// </summary>
    public bool CookieSecure { get; set; } = true;

    public LockoutSettings Lockout { get; set; } = new();

    public sealed class LockoutSettings
    {
        [Range(1, 100)] public int MaxFailedAttempts { get; set; } = 5;
        [Range(1, 1440)] public int Minutes { get; set; } = 15;
    }
}
