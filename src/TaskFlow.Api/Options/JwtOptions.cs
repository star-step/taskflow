using System.ComponentModel.DataAnnotations;

namespace TaskFlow.Api.Options;

public sealed class JwtOptions
{
    public const string Section = "Jwt";

    [Required] public string Issuer { get; set; } = "taskflow-api";
    [Required] public string Audience { get; set; } = "taskflow-web";

    /// <summary>HMAC-SHA256 key. Supply via environment or user-secrets; never commit a real one.</summary>
    [Required, MinLength(32, ErrorMessage = "Jwt:SigningKey must be at least 32 characters.")]
    public string SigningKey { get; set; } = string.Empty;

    [Range(1, 1440)] public int AccessTokenMinutes { get; set; } = 15;
}
