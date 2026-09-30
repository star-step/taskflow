using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using TaskFlow.Api.Tests.Support;

namespace TaskFlow.Api.Tests.Api;

/// <summary>
/// Enums are accepted only as a single declared name (case-insensitive). Comma-separated lists are
/// not treated as [Flags] combinations and numbers are rejected, in both JSON bodies and query strings.
/// </summary>
public sealed class EnumParsingTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public EnumParsingTests(ApiFactory factory) => _factory = factory;

    private static StringContent Json(string json) => new(json, Encoding.UTF8, "application/json");

    private async Task<HttpClient> Client()
    {
        var client = _factory.CreateClient();
        var register = await client.PostAsync("/api/auth/register",
            Json($$"""{"displayName":"Enum Tester","email":"enum{{Guid.NewGuid():N}}@company.com","password":"harbour-lantern-47"}"""));
        var token = JsonDocument.Parse(await register.Content.ReadAsStringAsync()).RootElement.GetProperty("accessToken").GetString();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private static async Task<JsonElement> Body(HttpResponseMessage r) =>
        JsonDocument.Parse(await r.Content.ReadAsStringAsync()).RootElement;

    [Theory]
    [InlineData("status=ToDo,InProgress", "status")]
    [InlineData("priority=Low,Medium", "priority")]
    [InlineData("priority=1", "priority")]
    [InlineData("status=In%20Progress", "status")]
    [InlineData("sortBy=Priority,Title", "sortBy")]
    public async Task Query_RejectsListsNumbersAndUnknownNames(string query, string field)
    {
        var response = await (await Client()).GetAsync($"/api/tasks?{query}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await Body(response);
        Assert.Equal("VALIDATION_FAILED", body.GetProperty("errorCode").GetString());
        Assert.True(body.GetProperty("errors").TryGetProperty(field, out _), $"expected an error for '{field}'");
    }

    [Fact]
    public async Task Query_RepeatedParametersAndAnyCase_StillWork()
    {
        var response = await (await Client()).GetAsync("/api/tasks?status=todo&status=InProgress&sortBy=title&sortDir=asc");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var statuses = (await Body(response)).EnumerateArray().Select(t => t.GetProperty("status").GetString()).Distinct().ToList();
        Assert.All(statuses, s => Assert.Contains(s, new[] { "ToDo", "InProgress" }));
        Assert.Contains("ToDo", statuses);
        Assert.Contains("InProgress", statuses);
    }

    [Theory]
    [InlineData("""{"title":"QA","priority":"Medium, High"}""", "priority")]
    [InlineData("""{"title":"QA","priority":"Low,Critical"}""", "priority")]
    [InlineData("""{"title":"QA","priority":3}""", "priority")]
    [InlineData("""{"title":"QA","priority":"2"}""", "priority")]
    [InlineData("""{"title":"QA","priority":"High","status":"ToDo, Done"}""", "status")]
    public async Task Body_RejectsListsAndNumbers(string json, string field)
    {
        var response = await (await Client()).PostAsync("/api/tasks", Json(json));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.True((await Body(response)).GetProperty("errors").TryGetProperty(field, out _), $"expected an error for '{field}'");
    }

    [Fact]
    public async Task Errors_ReadTheSame_InQueryAndBody()
    {
        var client = await Client();

        var query = await Body(await client.GetAsync("/api/tasks?priority=Urgent&sortBy=Nope"));
        var body = await Body(await client.PostAsync("/api/tasks", Json("""{"title":"QA","priority":"Urgent"}""")));

        const string priority = "Priority must be one of: Low, Medium, High, Critical.";
        Assert.Equal(priority, query.GetProperty("errors").GetProperty("priority")[0].GetString());
        Assert.Equal(priority, body.GetProperty("errors").GetProperty("priority")[0].GetString());
        Assert.Equal("Sort by must be one of: Priority, Modified, Created, Title, Status.",
            query.GetProperty("errors").GetProperty("sortBy")[0].GetString());
    }

    [Fact]
    public async Task Body_AcceptsNamesInAnyCase_AndResponsesUseNames()
    {
        var response = await (await Client()).PostAsync("/api/tasks", Json("""{"title":"QA case","priority":"high","status":"inprogress"}"""));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await Body(response);
        Assert.Equal("High", body.GetProperty("priority").GetString());
        Assert.Equal("InProgress", body.GetProperty("status").GetString());
    }
}
