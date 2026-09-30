using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using TaskFlow.Api.Tests.Support;

namespace TaskFlow.Api.Tests.Api;

/// <summary>Expected failures (404, 401, 400, 409) are normal traffic, not errors: nothing is logged at Error.</summary>
public sealed class ExceptionLoggingTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public ExceptionLoggingTests(ApiFactory factory) => _factory = factory;

    private static StringContent Json(string json) => new(json, Encoding.UTF8, "application/json");

    [Fact]
    public async Task ExpectedClientErrors_AreNotLoggedAsErrors()
    {
        var client = _factory.CreateClient();
        var email = $"logs{Guid.NewGuid():N}@company.com";
        var register = await client.PostAsync("/api/auth/register",
            Json($$"""{"displayName":"Log Tester","email":"{{email}}","password":"harbour-lantern-47"}"""));
        var token = JsonDocument.Parse(await register.Content.ReadAsStringAsync()).RootElement.GetProperty("accessToken").GetString();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/api/tasks/999")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsync("/api/tasks", Json("""{"title":" ","priority":"Low"}"""))).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsync("/api/auth/register",
            Json($$"""{"displayName":"Log Tester","email":"{{email}}","password":"harbour-lantern-47"}"""))).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsync("/api/auth/login",
            Json($$"""{"email":"{{email}}","password":"wrong-password-123"}"""))).StatusCode);

        var errors = _factory.Logs.Entries.Where(e => e.Level >= LogLevel.Error).Select(e => $"{e.Category}: {e.Message}").ToList();
        Assert.Empty(errors);
    }
}
