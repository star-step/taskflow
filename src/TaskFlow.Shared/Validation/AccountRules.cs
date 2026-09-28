namespace TaskFlow.Shared.Validation;

/// <summary>
/// Account rules shared by request validation, Identity options and the database configuration.
/// </summary>
public static class AccountRules
{
    public const int PasswordMinLength = 12;
    public const int PasswordMaxLength = 128;
    public const int DisplayNameMinLength = 2;
    public const int DisplayNameMaxLength = 100;
    public const int EmailMaxLength = 256;
}
