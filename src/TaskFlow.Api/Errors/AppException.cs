namespace TaskFlow.Api.Errors;

/// <summary>
/// Base for expected failures. <see cref="GlobalExceptionHandler"/> turns these into ProblemDetails
/// with the given status, title and error code; anything else becomes a 500.
/// </summary>
public abstract class AppException : Exception
{
    protected AppException(int statusCode, string errorCode, string title, string detail,
        IDictionary<string, string[]>? errors = null) : base(detail)
    {
        StatusCode = statusCode;
        ErrorCode = errorCode;
        Title = title;
        Errors = errors;
    }

    public int StatusCode { get; }
    public string ErrorCode { get; }
    public string Title { get; }

    /// <summary>Field-level messages keyed by camelCase field name, when relevant.</summary>
    public IDictionary<string, string[]>? Errors { get; }
}

public sealed class NotFoundException : AppException
{
    public NotFoundException(string resource, object id)
        : base(StatusCodes.Status404NotFound, ErrorCodes.TaskNotFound, $"{resource} not found",
            $"{resource} {id} does not exist or has been deleted.")
    {
    }
}

public sealed class DomainValidationException : AppException
{
    public DomainValidationException(string field, string message)
        : this(new Dictionary<string, string[]> { [field] = new[] { message } })
    {
    }

    public DomainValidationException(IDictionary<string, string[]> errors)
        : base(StatusCodes.Status400BadRequest, ErrorCodes.ValidationFailed, "Validation failed",
            "One or more fields are invalid.", errors)
    {
    }
}
