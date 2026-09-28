using Microsoft.AspNetCore.Identity;
using TaskFlow.Api.Domain;

namespace TaskFlow.Api.Security;

/// <summary>
/// Rejects easily guessed passwords that still pass the 12-character minimum: well-known long
/// passwords, keyboard or alphabet runs, a short unit repeated ("abcabcabcabc"), and passwords with
/// fewer than 4 distinct characters.
/// </summary>
public sealed class CommonPasswordValidator : IPasswordValidator<AppUser>
{
    public const string ErrorCode = "PasswordTooCommon";

    // Frequently breached passwords of 12+ characters (shorter ones are already rejected by length).
    private static readonly HashSet<string> Known = new(StringComparer.OrdinalIgnoreCase)
    {
        "123456789012", "1234567890123", "12345678901234", "123456789123", "123123123123",
        "111111111111", "000000000000", "121212121212", "987654321098", "098765432109",
        "password1234", "password12345", "password123456", "passwordpassword", "password1234!",
        "p@ssword1234", "p@ssw0rd1234", "passw0rd1234", "letmein12345", "welcome12345",
        "welcome123456", "iloveyou1234", "iloveyou12345", "qwerty123456", "qwertyuiop12",
        "qwertyuiopas", "qwertyuiopasdf", "qwertyuiopasdfgh", "qwertyuiopasdfghjkl", "asdfghjkl123",
        "asdfghjklqwe", "zxcvbnm12345", "1q2w3e4r5t6y", "1qaz2wsx3edc", "q1w2e3r4t5y6",
        "zaq12wsxcde3", "zaq1zaq1zaq1", "abcdefghijkl", "abcdefgh1234", "abc123abc123",
        "admin1234567", "administrator", "changeme1234", "trustno11234", "monkey123456",
        "football1234", "baseball1234", "superman1234", "starwars1234", "sunshine1234",
        "princess1234", "dragon123456", "master123456", "whatever1234", "internet1234",
        "computer1234", "michael12345", "jennifer1234", "charlie12345", "thomas123456",
        "summer202020", "winter202020", "spring202020", "autumn202020", "company12345",
        "taskflow1234", "taskflow12345", "taskflowtaskflow"
    };

    private static readonly string[] Sequences =
    {
        "abcdefghijklmnopqrstuvwxyz", "01234567890", "qwertyuiopasdfghjklzxcvbnm", "1qaz2wsx3edc4rfv5tgb"
    };

    public Task<IdentityResult> ValidateAsync(UserManager<AppUser> manager, AppUser user, string? password)
    {
        if (string.IsNullOrEmpty(password) || !IsWeak(password))
        {
            return Task.FromResult(IdentityResult.Success);
        }

        return Task.FromResult(IdentityResult.Failed(new IdentityError
        {
            Code = ErrorCode,
            Description = "This password is too easy to guess. Try a few unrelated words."
        }));
    }

    public static bool IsWeak(string password)
    {
        var lower = password.ToLowerInvariant();
        if (Known.Contains(lower) || lower.Distinct().Count() < 4)
        {
            return true;
        }

        // A short unit repeated to fill the length, e.g. "abcabcabcabc" or "hello!hello!".
        for (var unit = 1; unit <= lower.Length / 2; unit++)
        {
            if (lower.Length % unit == 0 && string.Concat(Enumerable.Repeat(lower[..unit], lower.Length / unit)) == lower)
            {
                return true;
            }
        }

        // Straight runs along the alphabet, digits or keyboard, forwards or backwards.
        foreach (var sequence in Sequences)
        {
            var reversed = new string(sequence.Reverse().ToArray());
            if (sequence.Contains(lower, StringComparison.Ordinal) || reversed.Contains(lower, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }
}
