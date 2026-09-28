using Microsoft.AspNetCore.Identity;
using TaskFlow.Api.Domain;

namespace TaskFlow.Api.Security;

/// <summary>Rejects passwords that contain the user's email name or any part of their display name.</summary>
public sealed class PersonalInfoPasswordValidator : IPasswordValidator<AppUser>
{
    public const string ErrorCode = "PasswordContainsPersonalInfo";
    private const int MinPartLength = 3;

    public Task<IdentityResult> ValidateAsync(UserManager<AppUser> manager, AppUser user, string? password)
    {
        if (string.IsNullOrEmpty(password) || !ContainsPersonalInfo(password, user.Email, user.DisplayName))
        {
            return Task.FromResult(IdentityResult.Success);
        }

        return Task.FromResult(IdentityResult.Failed(new IdentityError
        {
            Code = ErrorCode,
            Description = "Password can't include your name or email."
        }));
    }

    public static bool ContainsPersonalInfo(string password, string? email, string? displayName)
    {
        var parts = new List<string>();
        var local = email?.Split('@')[0];
        if (!string.IsNullOrEmpty(local))
        {
            parts.Add(local!);
            parts.AddRange(local!.Split('.', '_', '-', '+'));
        }

        if (!string.IsNullOrWhiteSpace(displayName))
        {
            parts.AddRange(displayName!.Split(' ', StringSplitOptions.RemoveEmptyEntries));
        }

        return parts
            .Where(p => p.Length >= MinPartLength)
            .Any(p => password.Contains(p, StringComparison.OrdinalIgnoreCase));
    }
}
