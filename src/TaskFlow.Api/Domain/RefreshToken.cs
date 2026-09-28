namespace TaskFlow.Api.Domain;

/// <summary>
/// A refresh token issued to a browser session. Only a SHA-256 hash is stored; the raw value lives
/// in the user's httpOnly cookie. Tokens rotate on every use, and all tokens descended from one
/// sign-in share a <see cref="FamilyId"/> so a replayed token can revoke the whole chain.
/// </summary>
public class RefreshToken
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public AppUser? User { get; set; }

    public string TokenHash { get; set; } = string.Empty;
    public Guid FamilyId { get; set; }

    /// <summary>The user's security stamp when issued. A password change alters the stamp and invalidates the token.</summary>
    public string SecurityStamp { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime? RevokedAt { get; set; }
    public string? ReplacedByTokenHash { get; set; }
}
