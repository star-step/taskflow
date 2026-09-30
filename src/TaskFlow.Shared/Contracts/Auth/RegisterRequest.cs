using System.ComponentModel.DataAnnotations;
using TaskFlow.Shared.Validation;

namespace TaskFlow.Shared.Contracts.Auth;

public class RegisterRequest
{
    [Required(ErrorMessage = "Full name is required.")]
    [NotWhiteSpace(ErrorMessage = "Full name is required.")]
    [StringLength(AccountRules.DisplayNameMaxLength, MinimumLength = AccountRules.DisplayNameMinLength,
        ErrorMessage = "Full name must be between {2} and {1} characters.")]
    public string DisplayName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Enter a valid email address.")]
    [StringLength(AccountRules.EmailMaxLength, ErrorMessage = "Email must be {1} characters or fewer.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required.")]
    [MinLength(AccountRules.PasswordMinLength, ErrorMessage = "Password must be at least {1} characters.")]
    [MaxLength(AccountRules.PasswordMaxLength, ErrorMessage = "Password must be {1} characters or fewer.")]
    public string Password { get; set; } = string.Empty;
}
