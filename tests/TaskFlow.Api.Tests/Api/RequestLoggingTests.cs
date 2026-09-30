using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using TaskFlow.Api.Tests.Support;

namespace TaskFlow.Api.Tests.Api;

/// <summary>
/// Request logging records method, path, status, elapsed time and the same traceId the client sees in
/// ProblemDetails, and never request bodies, headers or query strings.
/// </summary>
public sealed class RequestLoggingTests : IClassFixture<ApiFactory>
{
    private const string Category = "TaskFlow.Api.Infrastructure.RequestLoggingMiddleware";
    private readonly ApiFactory _factory;

    public RequestLoggingTests(ApiFactory factory) => _factory = factory;

    private IReadOnlyList<CapturingLoggerProvider.LogEntry> RequestLogs(string path) =>
        _factory.Logs.Entries.Where(e => e.Category == Category && Equals(e.State.GetValueOrDefault("Path"), path)).ToList();

    [Fact]
    public async Task EachRequest_LogsMethodPathStatusElapsedAndTraceId()
    {
        var response = await _factory.CreateClient().GetAsync("/api/tasks/7");
        var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;

        var entry = Assert.Single(RequestLogs("/api/tasks/7"));
        Assert.Equal(LogLevel.Information, entry.Level);
        Assert.Equal("GET", entry.State["Method"]);
        Assert.Equal(401, entry.State["StatusCode"]);
        Assert.IsType<double>(entry.State["ElapsedMs"]);
        Assert.True((double)entry.State["ElapsedMs"]! >= 0);
        // Same id as the ProblemDetails traceId, so a reported error can be found in the logs.
        Assert.Equal(body.GetProperty("traceId").GetString(), entry.State["TraceId"]);
        Assert.Matches(@"^HTTP GET /api/tasks/7 responded 401 in \d+(\.\d+)? ms \(traceId 00-[0-9a-f]{32}-[0-9a-f]{16}-0[01]\)$", entry.Message);
    }

    [Fact]
    public async Task Logs_NeverContainBodiesAuthHeadersOrQueryStrings()
    {
        var client = _factory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/login?note=query-secret-123")
        {
            Content = new StringContent("""{"email":"nobody@company.com","password":"body-secret-456"}""", Encoding.UTF8, "application/json")
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "header-secret-789");

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Single(RequestLogs("/api/auth/login"));
        var everything = string.Join("\n", _factory.Logs.Entries.Select(e =>
            e.Message + " " + string.Join(" ", e.State.Select(kv => $"{kv.Key}={kv.Value}"))));
        Assert.DoesNotContain("query-secret-123", everything);
        Assert.DoesNotContain("body-secret-456", everything);
        Assert.DoesNotContain("header-secret-789", everything);
    }

    [Fact]
    public async Task HealthChecks_LogAtDebugOnly()
    {
        await _factory.CreateClient().GetAsync("/health/live");

        Assert.Equal(LogLevel.Debug, Assert.Single(RequestLogs("/health/live")).Level);
    }
}
