using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Testcontainers.MsSql;

namespace TaskFlow.Api.Tests.Integration;

/// <summary>
/// GET /api/tasks/summary end to end: the real API (migrations applied on startup) on SQL Server.
/// The raw-SQL summary must agree with the EF-backed list after creates and soft deletes.
/// </summary>
[Trait("Category", "Integration")]
public sealed class SummaryEndpointTests : IAsyncLifetime
{
    private MsSqlContainer? _sql;
    private string? _skipReason;
    private WebApplicationFactory<Program>? _app;

    public async Task InitializeAsync()
    {
        (_sql, _skipReason) = await SqlServerContainer.StartAsync();
        if (_sql is null)
        {
            return;
        }

        _app = new WebApplicationFactory<Program>().WithWebHostBuilder(b =>
        {
            b.UseEnvironment("Testing");
            b.UseSetting("ConnectionStrings:TaskFlow", _sql.TaskFlowDatabase());
            b.UseSetting("Database:MigrateOnStartup", "true");
            b.UseSetting("Jwt:SigningKey", "integration-test-signing-key-0123456789abcdef");
            b.UseSetting("Auth:AllowedEmailDomains:0", "company.com");
            b.UseSetting("Auth:CookieSecure", "false");
        });
    }

    public async Task DisposeAsync()
    {
        if (_app is not null)
        {
            await _app.DisposeAsync();
        }

        if (_sql is not null)
        {
            await _sql.DisposeAsync();
        }
    }

    private static StringContent Json(string json) => new(json, Encoding.UTF8, "application/json");

    [SkippableFact]
    public async Task Summary_OverHttp_MatchesTheList_AfterCreateAndSoftDelete()
    {
        Skip.If(_skipReason is not null, _skipReason);
        var client = _app!.CreateClient();
        var register = await client.PostAsync("/api/auth/register",
            Json("""{"displayName":"Integration User","email":"integration.user@company.com","password":"harbour-lantern-47"}"""));
        Assert.Equal(HttpStatusCode.Created, register.StatusCode);
        var token = JsonDocument.Parse(await register.Content.ReadAsStringAsync()).RootElement.GetProperty("accessToken").GetString();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        Assert.Equal(HttpStatusCode.Created, (await client.PostAsync("/api/tasks",
            Json("""{"title":"Integration task","priority":"Critical","status":"InProgress"}"""))).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync("/api/tasks/4")).StatusCode); // To Do / Low

        var summary = JsonDocument.Parse(await client.GetStringAsync("/api/tasks/summary")).RootElement;
        var list = JsonDocument.Parse(await client.GetStringAsync("/api/tasks")).RootElement.EnumerateArray().ToList();

        Assert.Equal(12, summary.GetProperty("total").GetInt32()); // 12 seeded + 1 created - 1 deleted
        Assert.Equal(list.Count, summary.GetProperty("total").GetInt32());
        foreach (var status in new[] { "ToDo", "InProgress", "Done" })
        {
            Assert.Equal(list.Count(t => t.GetProperty("status").GetString() == status),
                summary.GetProperty("byStatus").GetProperty(status).GetInt32());
        }

        foreach (var priority in new[] { "Low", "Medium", "High", "Critical" })
        {
            Assert.Equal(list.Count(t => t.GetProperty("priority").GetString() == priority),
                summary.GetProperty("byPriority").GetProperty(priority).GetInt32());
        }

        Assert.Equal(list.Count, summary.GetProperty("matrix").EnumerateArray().Sum(c => c.GetProperty("count").GetInt32()));
        Assert.Equal(4, summary.GetProperty("byStatus").GetProperty("ToDo").GetInt32());
        Assert.Equal(4, summary.GetProperty("byPriority").GetProperty("Critical").GetInt32());
    }
}
