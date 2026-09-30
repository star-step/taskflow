using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using TaskFlow.Api.Infrastructure;
using TaskFlow.Api.Tests.Support;

namespace TaskFlow.Api.Tests.Api;

public sealed class RequestLoggingMiddlewareTests
{
    [Fact]
    public async Task ExceptionEscapingThePipeline_IsLoggedAtError_AndRethrown()
    {
        // E.g. the response had already started, so the exception handler couldn't run and
        // ExceptionHandlerMiddleware's own log line is filtered out (see Program.cs).
        var logs = new CapturingLoggerProvider();
        var logger = new LoggerFactory(new[] { logs }).CreateLogger<RequestLoggingMiddleware>();
        var middleware = new RequestLoggingMiddleware(_ => throw new InvalidOperationException("boom after start"), logger);
        var context = new DefaultHttpContext();
        context.Request.Method = "GET";
        context.Request.Path = "/api/tasks";

        await Assert.ThrowsAsync<InvalidOperationException>(() => middleware.InvokeAsync(context));

        var error = Assert.Single(logs.Entries, e => e.Level == LogLevel.Error);
        Assert.Contains("GET /api/tasks", error.Message);
    }
}
