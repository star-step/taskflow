using Microsoft.AspNetCore.Identity;

namespace TaskFlow.Api.Domain;

public class AppUser : IdentityUser<int>
{
    public string DisplayName { get; set; } = string.Empty;
    public DateTime CreatedDate { get; set; }

    public List<RefreshToken> RefreshTokens { get; set; } = new();
}
