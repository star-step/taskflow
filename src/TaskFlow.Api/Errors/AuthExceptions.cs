namespace TaskFlow.Api.Errors;

public sealed class InvalidCredentialsException : AppException
{
    private InvalidCredentialsException(string code, string detail)
        : base(StatusCodes.Status401Unauthorized, code, "Sign-in failed", detail)
    {
    }

    /// <summary>Same message for an unknown email and a wrong password, so neither is revealed.</summary>
    public static InvalidCredentialsException Wrong(int? attemptsLeft) => new(ErrorCodes.InvalidCredentials,
        attemptsLeft is > 0 and <= 3
            ? $"Email or password is incorrect. {attemptsLeft} attempt{(attemptsLeft == 1 ? "" : "s")} left before the account is locked for a while."
            : "Email or password is incorrect.");

    public static InvalidCredentialsException LockedOut(DateTimeOffset until) => new(ErrorCodes.LockedOut,
        $"Too many failed attempts. Try again after {until.UtcDateTime:HH:mm} UTC.");
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
