using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace TaskFlow.Api.Errors;

/// <summary>
/// Converts every unhandled exception into an RFC 7807 ProblemDetails response with an
/// <c>errorCode</c> and <c>traceId</c>. Unexpected errors are logged with the same traceId and
/// only reveal their message in Development.
/// </summary>
public sealed class GlobalExceptionHandler : IExceptionHandler
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

        var problem = Map(exception);
        if (problem.Status >= StatusCodes.Status500InternalServerError)
        {
            _logger.LogError(exception, "Unhandled exception for {Method} {Path}",
                httpContext.Request.Method, httpContext.Request.Path);
        }

        httpContext.Response.StatusCode = problem.Status!.Value;
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
            case SqlException:
            case DbUpdateException { InnerException: SqlException }:
                return Problem(StatusCodes.Status503ServiceUnavailable, "Database unavailable",
                    "The database could not complete the request. Try again shortly.", ErrorCodes.DatabaseUnavailable);
            default:
                return Problem(StatusCodes.Status500InternalServerError, "Something went wrong",
                    _env.IsDevelopment() ? exception.Message : "An unexpected error occurred. Quote the traceId if you report it.",
                    ErrorCodes.Unexpected);
        }
    }

    private static ProblemDetails Problem(int status, string title, string detail, string code)
    {
        var problem = new ProblemDetails { Status = status, Title = title, Detail = detail };
        problem.Extensions["errorCode"] = code;
        return problem;
    }
}
