namespace TaskFlow.Api.Errors;

/// <summary>Machine-readable codes returned in every ProblemDetails body as <c>errorCode</c>.</summary>
public static class ErrorCodes
{
    public const string ValidationFailed = "VALIDATION_FAILED";
    public const string TaskNotFound = "TASK_NOT_FOUND";
    public const string NotFound = "NOT_FOUND";
    public const string ConcurrencyConflict = "CONCURRENCY_CONFLICT";
    public const string DatabaseUnavailable = "DATABASE_UNAVAILABLE";
    public const string Unexpected = "UNEXPECTED_ERROR";
    public const string Unauthenticated = "UNAUTHENTICATED";
    public const string Forbidden = "FORBIDDEN";
    public const string RateLimited = "RATE_LIMITED";
    public const string InvalidCredentials = "INVALID_CREDENTIALS";
    public const string LockedOut = "LOCKED_OUT";
    public const string EmailTaken = "EMAIL_TAKEN";
    public const string EmailDomainNotAllowed = "EMAIL_DOMAIN_NOT_ALLOWED";
    public const string InvalidRefreshToken = "INVALID_REFRESH_TOKEN";
    public const string ClientHeaderMissing = "CLIENT_HEADER_MISSING";
    public const string MethodNotAllowed = "METHOD_NOT_ALLOWED";
    public const string UnsupportedMediaType = "UNSUPPORTED_MEDIA_TYPE";
    public const string BadRequest = "BAD_REQUEST";

    public static string ForStatus(int status) => status switch
    {
        StatusCodes.Status400BadRequest => ValidationFailed,
        StatusCodes.Status401Unauthorized => Unauthenticated,
        StatusCodes.Status403Forbidden => Forbidden,
        StatusCodes.Status404NotFound => NotFound,
        StatusCodes.Status405MethodNotAllowed => MethodNotAllowed,
        StatusCodes.Status415UnsupportedMediaType => UnsupportedMediaType,
        StatusCodes.Status409Conflict => ConcurrencyConflict,
        StatusCodes.Status429TooManyRequests => RateLimited,
        StatusCodes.Status503ServiceUnavailable => DatabaseUnavailable,
        >= 400 and < 500 => BadRequest,
        _ => Unexpected
    };
}
