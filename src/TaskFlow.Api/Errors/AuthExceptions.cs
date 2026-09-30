namespace TaskFlow.Api.Errors;

/// <summary>
/// The one response for every failed sign-in: unknown email, wrong password, or locked account.
/// Anything that differed between those cases (an attempts-left count, a LOCKED_OUT code, an unlock
/// time) would tell a caller which emails have accounts.
/// </summary>
public sealed class InvalidCredentialsException : AppException
{
    public InvalidCredentialsException()
        : base(StatusCodes.Status401Unauthorized, ErrorCodes.InvalidCredentials, "Sign-in failed",
            "Email or password is incorrect. After several failed attempts, sign-in is paused for 15 minutes.")
    {
    }
}

public sealed class EmailAlreadyRegisteredException : AppException
{
    public EmailAlreadyRegisteredException()
        : base(StatusCodes.Status409Conflict, ErrorCodes.EmailTaken, "Email already registered",
            "An account with this email already exists.",
            new Dictionary<string, string[]> { ["email"] = new[] { "An account with this email already exists." } })
    {
    }
}

public sealed class EmailDomainNotAllowedException : AppException
{
    public EmailDomainNotAllowedException(IEnumerable<string> allowedDomains)
        : base(StatusCodes.Status403Forbidden, ErrorCodes.EmailDomainNotAllowed, "Email domain not allowed",
            "Registration is limited to company email addresses.",
            new Dictionary<string, string[]>
            {
                ["email"] = new[] { $"Use your {string.Join(" or ", allowedDomains.Select(d => "@" + d))} email." }
            })
    {
    }
}

public sealed class InvalidRefreshTokenException : AppException
{
    public InvalidRefreshTokenException()
        : base(StatusCodes.Status401Unauthorized, ErrorCodes.InvalidRefreshToken, "Session expired",
            "Your session has expired. Sign in again.")
    {
    }
}

public sealed class ClientHeaderMissingException : AppException
{
    public ClientHeaderMissingException(string header)
        : base(StatusCodes.Status403Forbidden, ErrorCodes.ClientHeaderMissing, "Request refused",
            $"This endpoint requires the {header} header.")
    {
    }
}
