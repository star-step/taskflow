using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace TaskFlow.Api.Errors;

/// <summary>
/// Converts every unhandled exception into an RFC 7807 ProblemDetails response with an
/// <c>errorCode</c> and <c>traceId</c>. Unexpected errors are logged with the same traceId and
/// only reveal their message in Development.
/// </summary>
public sealed partial class GlobalExceptionHandler : IExceptionHandler
{
    private readonly IProblemDetailsService _problemDetails;
    private readonly ILogger<GlobalExceptionHandler> _logger;
    private readonly IHostEnvironment _env;

    public GlobalExceptionHandler(IProblemDetailsService problemDetails, ILogger<GlobalExceptionHandler> logger,
        IHostEnvironment env)
    {
        _problemDetails = problemDetails;
        _logger = logger;
        _env = env;
    }

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is OperationCanceledException && httpContext.RequestAborted.IsCancellationRequested)
        {
            // The client went away; there is nobody to respond to.
            return true;
        }

        // This handler is the only place exceptions are logged: ExceptionHandlerMiddleware's own
        // Error entry is filtered out in Program.cs, since most exceptions here are expected 4xx.
        var problem = Map(exception);
        var status = problem.Status!.Value;
        var method = httpContext.Request.Method;
        var path = httpContext.Request.Path.Value ?? "/";
        if (status >= StatusCodes.Status500InternalServerError)
        {
            LogUnexpected(_logger, exception, method, path, status);
        }
        else
        {
            LogExpected(_logger, method, path, status, problem.Extensions["errorCode"] as string ?? ErrorCodes.ForStatus(status));
        }

        httpContext.Response.StatusCode = status;
        return await _problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problem,
            Exception = exception
        });
    }

    private ProblemDetails Map(Exception exception)
    {
        switch (exception)
        {
            case AppException app:
            {
                var problem = app.Errors is null
                    ? new ProblemDetails()
                    : new ValidationProblemDetails(app.Errors);
                problem.Status = app.StatusCode;
                problem.Title = app.Title;
                problem.Detail = app.Message;
                problem.Extensions["errorCode"] = app.ErrorCode;
                return problem;
            }
            case BadHttpRequestException bad:
                return Problem(bad.StatusCode, "Bad request", bad.Message, ErrorCodes.ValidationFailed);
            case DbUpdateConcurrencyException:
                return Problem(StatusCodes.Status409Conflict, "Conflict",
                    "The task was changed by someone else. Reload and try again.", ErrorCodes.ConcurrencyConflict);
            case DbUpdateException { InnerException: SqlException sql } when SqlErrors.Classify(sql.Number, sql.Class) == SqlErrorKind.UniqueViolation:
                return Problem(StatusCodes.Status409Conflict, "Conflict",
                    "A record with the same unique value already exists.", ErrorCodes.DuplicateValue);
            case DbUpdateException { InnerException: SqlException sql } when SqlErrors.Classify(sql.Number, sql.Class) == SqlErrorKind.ConstraintViolation:
                return Problem(StatusCodes.Status409Conflict, "Conflict",
                    "The change conflicts with existing data.", ErrorCodes.ConstraintViolation);
            case RetryLimitExceededException:
            case SqlException sqlError when SqlErrors.Classify(sqlError.Number, sqlError.Class) == SqlErrorKind.Transient:
            case DbUpdateException { InnerException: SqlException inner } when SqlErrors.Classify(inner.Number, inner.Class) == SqlErrorKind.Transient:
                return Problem(StatusCodes.Status503ServiceUnavailable, "Database unavailable",
                    "The database could not complete the request. Try again shortly.", ErrorCodes.DatabaseUnavailable);
            default:
                return Problem(StatusCodes.Status500InternalServerError, "Something went wrong",
                    _env.IsDevelopment() ? exception.Message : "An unexpected error occurred. Quote the traceId if you report it.",
                    ErrorCodes.Unexpected);
        }
    }

    [LoggerMessage(EventId = 2000, Level = LogLevel.Error, Message = "Unhandled exception for {Method} {Path}, responding {StatusCode}")]
    private static partial void LogUnexpected(ILogger logger, Exception exception, string method, string path, int statusCode);

    [LoggerMessage(EventId = 2001, Level = LogLevel.Debug, Message = "{Method} {Path} handled as {StatusCode} {ErrorCode}")]
    private static partial void LogExpected(ILogger logger, string method, string path, int statusCode, string errorCode);

    private static ProblemDetails Problem(int status, string title, string detail, string code)
    {
        var problem = new ProblemDetails { Status = status, Title = title, Detail = detail };
        problem.Extensions["errorCode"] = code;
        return problem;
    }
}
