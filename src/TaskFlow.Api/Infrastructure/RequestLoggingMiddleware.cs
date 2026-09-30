using System.Diagnostics;

namespace TaskFlow.Api.Infrastructure;

/// <summary>
/// One structured log line per request: method, path, status, elapsed time and the traceId that is
/// also returned in ProblemDetails, so a user-reported error can be found in the logs.
/// Deliberately never logs query strings, headers or bodies (tokens, passwords, search text).
/// Health probes log at Debug so Docker's health checks don't flood the output.
/// </summary>
public sealed partial class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var start = Stopwatch.GetTimestamp();
        try
        {
            await _next(context);
        }
        finally
        {
            var elapsedMs = Math.Round(Stopwatch.GetElapsedTime(start).TotalMilliseconds, 1);
            var method = context.Request.Method;
            var path = context.Request.Path.HasValue ? context.Request.Path.Value! : "/";
            var status = context.Response.StatusCode;
            var traceId = Activity.Current?.Id ?? context.TraceIdentifier;

            if (path.StartsWith("/health", StringComparison.OrdinalIgnoreCase))
            {
                LogHealthProbe(_logger, method, path, status, elapsedMs, traceId);
            }
            else if (status >= StatusCodes.Status500InternalServerError)
            {
                LogServerError(_logger, method, path, status, elapsedMs, traceId);
            }
            else
            {
                LogRequest(_logger, method, path, status, elapsedMs, traceId);
            }
        }
    }

    private const string Template = "HTTP {Method} {Path} responded {StatusCode} in {ElapsedMs} ms (traceId {TraceId})";

    [LoggerMessage(EventId = 1000, Level = LogLevel.Information, Message = Template)]
    private static partial void LogRequest(ILogger logger, string method, string path, int statusCode, double elapsedMs, string traceId);

    [LoggerMessage(EventId = 1001, Level = LogLevel.Warning, Message = Template)]
    private static partial void LogServerError(ILogger logger, string method, string path, int statusCode, double elapsedMs, string traceId);

    [LoggerMessage(EventId = 1002, Level = LogLevel.Debug, Message = Template)]
    private static partial void LogHealthProbe(ILogger logger, string method, string path, int statusCode, double elapsedMs, string traceId);
}
