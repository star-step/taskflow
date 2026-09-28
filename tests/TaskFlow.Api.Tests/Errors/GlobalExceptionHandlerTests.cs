using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using TaskFlow.Api.Errors;

namespace TaskFlow.Api.Tests.Errors;

public sealed class GlobalExceptionHandlerTests
{
    private static async Task<(int Status, JsonElement Body)> Handle(Exception exception, string environment = "Production")
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddTaskFlowProblemDetails();
        var provider = services.BuildServiceProvider();

        var context = new DefaultHttpContext { RequestServices = provider };
        context.Request.Path = "/api/tasks/42";
        context.Response.Body = new MemoryStream();

        var env = new FakeEnvironment { EnvironmentName = environment };
        var handler = new GlobalExceptionHandler(provider.GetRequiredService<IProblemDetailsService>(),
            NullLogger<GlobalExceptionHandler>.Instance, env);

        Assert.True(await handler.TryHandleAsync(context, exception, CancellationToken.None));

        context.Response.Body.Position = 0;
        var body = await JsonDocument.ParseAsync(context.Response.Body);
        return (context.Response.StatusCode, body.RootElement.Clone());
    }

    [Fact]
    public async Task MapsNotFound_To404ProblemDetails()
    {
        var (status, body) = await Handle(new NotFoundException("Task", 42));

        Assert.Equal(404, status);
        Assert.Equal(404, body.GetProperty("status").GetInt32());
        Assert.Equal("TASK_NOT_FOUND", body.GetProperty("errorCode").GetString());
        Assert.Equal("Task 42 does not exist or has been deleted.", body.GetProperty("detail").GetString());
        Assert.Equal("/api/tasks/42", body.GetProperty("instance").GetString());
        Assert.False(string.IsNullOrEmpty(body.GetProperty("traceId").GetString()));
    }

    [Fact]
    public async Task MapsDomainValidation_To400WithFieldErrors()
    {
        var (status, body) = await Handle(new DomainValidationException("title", "Title is required."));

        Assert.Equal(400, status);
        Assert.Equal("VALIDATION_FAILED", body.GetProperty("errorCode").GetString());
        Assert.Equal("Title is required.", body.GetProperty("errors").GetProperty("title")[0].GetString());
    }

    [Fact]
    public async Task UnexpectedException_Returns500_WithoutLeakingMessageOutsideDevelopment()
    {
        var (status, body) = await Handle(new InvalidOperationException("connection string has password=hunter2"));

        Assert.Equal(500, status);
        Assert.Equal("UNEXPECTED_ERROR", body.GetProperty("errorCode").GetString());
        Assert.DoesNotContain("hunter2", body.GetProperty("detail").GetString());
    }

    private sealed class FakeEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = "Production";
        public string ApplicationName { get; set; } = "TaskFlow.Api";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } =
            new Microsoft.Extensions.FileProviders.NullFileProvider();
    }
}
