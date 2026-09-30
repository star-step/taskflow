using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using TaskFlow.Api.Tests.Support;

namespace TaskFlow.Api.Tests.Api;

/// <summary>End-to-end HTTP checks of routing, auth, validation and the error format.</summary>
public sealed class ApiPipelineTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public ApiPipelineTests(ApiFactory factory) => _factory = factory;

    private static StringContent Json(string json) => new(json, Encoding.UTF8, "application/json");

    private static async Task<JsonElement> Body(HttpResponseMessage response) =>
        (await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync())).RootElement.Clone();

    private async Task<(HttpClient Client, HttpResponseMessage Register)> SignedInClient()
    {
        var client = _factory.CreateClient(new() { HandleCookies = true, BaseAddress = new Uri("http://localhost") });
        var email = $"user{Guid.NewGuid():N}@company.com";
        var register = await client.PostAsync("/api/auth/register",
            Json($$"""{"displayName":"Test User","email":"{{email}}","password":"harbour-lantern-47"}"""));
        Assert.Equal(HttpStatusCode.Created, register.StatusCode);
        var token = (await Body(register)).GetProperty("accessToken").GetString();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return (client, register);
    }

    [Fact]
    public async Task Unauthenticated_Returns401ProblemDetails()
    {
        var response = await _factory.CreateClient().GetAsync("/api/tasks");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal("UNAUTHENTICATED", (await Body(response)).GetProperty("errorCode").GetString());
    }

    [Fact]
    public async Task Register_SetsHttpOnlyStrictRefreshCookie()
    {
        var (_, register) = await SignedInClient();

        var cookie = Assert.Single(register.Headers.GetValues("Set-Cookie"));
        Assert.StartsWith("tf_refresh=", cookie);
        Assert.Contains("httponly", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=strict", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("path=/api/auth", cookie, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task InvalidCreate_ReturnsOurValidationShape()
    {
        var (client, _) = await SignedInClient();

        var blankTitle = await client.PostAsync("/api/tasks", Json("""{"title":"  ","priority":"High"}"""));
        var badEnum = await client.PostAsync("/api/tasks", Json("""{"title":"Ok","priority":"Urgent"}"""));

        Assert.Equal(HttpStatusCode.BadRequest, blankTitle.StatusCode);
        var body = await Body(blankTitle);
        Assert.Equal("Validation failed", body.GetProperty("title").GetString());
        Assert.Equal("VALIDATION_FAILED", body.GetProperty("errorCode").GetString());
        Assert.True(body.TryGetProperty("traceId", out _));
        Assert.Equal("Title is required.", body.GetProperty("errors").GetProperty("title")[0].GetString());

        Assert.Equal(HttpStatusCode.BadRequest, badEnum.StatusCode);
        var errors = (await Body(badEnum)).GetProperty("errors");
        Assert.Equal("Priority must be one of: Low, Medium, High, Critical.", errors.GetProperty("priority")[0].GetString());
        Assert.False(errors.TryGetProperty("request", out _));
    }

    [Fact]
    public async Task MalformedJson_GetsOneClearMessage()
    {
        var (client, _) = await SignedInClient();

        var response = await client.PostAsync("/api/tasks", Json("""{"title": "unterminated"""));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var errors = (await Body(response)).GetProperty("errors");
        Assert.Equal(new[] { "request" }, errors.EnumerateObject().Select(p => p.Name));
        Assert.Equal(new[] { "The request body is not valid JSON." },
            errors.GetProperty("request").EnumerateArray().Select(e => e.GetString()));
    }

    [Fact]
    public async Task AuthResponse_ExpiresAt_IsWholeSeconds_LikeTheJwtExpClaim()
    {
        var register = await _factory.CreateClient().PostAsync("/api/auth/register",
            Json($$"""{"displayName":"Expiry Check","email":"exp{{Guid.NewGuid():N}}@company.com","password":"harbour-lantern-47"}"""));

        var expiresAt = (await Body(register)).GetProperty("expiresAt").GetString()!;

        Assert.EndsWith("Z", expiresAt);
        Assert.DoesNotContain(".", expiresAt);
    }

    [Fact]
    public async Task TaskLifecycle_CreatePatchDelete()
    {
        var (client, _) = await SignedInClient();

        var created = await client.PostAsync("/api/tasks", Json("""{"title":"Pipeline task","priority":"Low"}"""));
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var id = (await Body(created)).GetProperty("id").GetInt32();
        Assert.Equal($"/api/tasks/{id}", created.Headers.Location?.AbsolutePath);

        var patched = await client.PatchAsync($"/api/tasks/{id}/status", Json("""{"status":"Done"}"""));
        Assert.Equal("Done", (await Body(patched)).GetProperty("status").GetString());

        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/api/tasks/{id}")).StatusCode);
        var gone = await client.GetAsync($"/api/tasks/{id}");
        Assert.Equal(HttpStatusCode.NotFound, gone.StatusCode);
        Assert.Equal("TASK_NOT_FOUND", (await Body(gone)).GetProperty("errorCode").GetString());
    }

    [Fact]
    public async Task List_FiltersByQueryString_AndRejectsUnknownValues()
    {
        var (client, _) = await SignedInClient();

        var list = await client.GetFromJsonAsync<JsonElement>("/api/tasks?status=InProgress&priority=High&priority=Critical");
        Assert.Equal(new[] { 3, 10, 1 }, list.EnumerateArray().Select(t => t.GetProperty("id").GetInt32()));

        var bad = await client.GetAsync("/api/tasks?status=Bogus");
        Assert.Equal(HttpStatusCode.BadRequest, bad.StatusCode);
    }

    [Fact]
    public async Task WrongMethod_Returns405WithErrorCode()
    {
        var (client, _) = await SignedInClient();

        var response = await client.PatchAsync("/api/tasks/1", Json("""{"status":"Done"}"""));

        Assert.Equal(HttpStatusCode.MethodNotAllowed, response.StatusCode);
        Assert.Equal("METHOD_NOT_ALLOWED", (await Body(response)).GetProperty("errorCode").GetString());
    }

    [Fact]
    public async Task Refresh_RequiresClientHeader_AndUsesCookie()
    {
        var (client, _) = await SignedInClient();

        var noHeader = await client.PostAsync("/api/auth/refresh", null);
        Assert.Equal(HttpStatusCode.Forbidden, noHeader.StatusCode);
        Assert.Equal("CLIENT_HEADER_MISSING", (await Body(noHeader)).GetProperty("errorCode").GetString());

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/refresh");
        request.Headers.Add("X-TaskFlow-Client", "web");
        var refreshed = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, refreshed.StatusCode);
        Assert.Equal("Test User", (await Body(refreshed)).GetProperty("user").GetProperty("displayName").GetString());
    }
}
