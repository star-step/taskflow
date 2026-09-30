using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using TaskFlow.Api.Tests.Support;

namespace TaskFlow.Api.Tests.Api;

/// <summary>Token validation, rate limiting and the users endpoint, over real HTTP.</summary>
public sealed class SecurityPipelineTests : IClassFixture<ApiFactory>
{
    // Must match ApiFactory's settings.
    private const string Key = "pipeline-test-signing-key-0123456789abcdef";
    private readonly ApiFactory _factory;

    public SecurityPipelineTests(ApiFactory factory) => _factory = factory;

    private static StringContent Json(string json) => new(json, Encoding.UTF8, "application/json");

    private static string Token(string key = Key, string issuer = "taskflow-api", string audience = "taskflow-web",
        DateTime? expires = null, string sub = "1")
    {
        var now = DateTime.UtcNow;
        var exp = expires ?? now.AddMinutes(10);
        return new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Issuer = issuer,
            Audience = audience,
            IssuedAt = exp.AddMinutes(-15),
            NotBefore = exp.AddMinutes(-15),
            Expires = exp,
            Subject = new ClaimsIdentity(new[] { new Claim("sub", sub), new Claim("name", "Test") }),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)), SecurityAlgorithms.HmacSha256)
        });
    }

    private async Task<HttpStatusCode> GetTasksWith(string token)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return (await client.GetAsync("/api/tasks")).StatusCode;
    }

    [Fact]
    public async Task ValidToken_IsAccepted() => Assert.Equal(HttpStatusCode.OK, await GetTasksWith(Token()));

    public static TheoryData<string, string> TamperedTokens()
    {
        var valid = Token();
        var parts = valid.Split('.');
        var unsignedHeader = Base64UrlEncoder.Encode("""{"alg":"none","typ":"JWT"}""");
        return new TheoryData<string, string>
        {
            { "wrong signing key", Token(key: "some-other-signing-key-0123456789abcdef-xyz") },
            { "wrong audience", Token(audience: "someone-else") },
            { "wrong issuer", Token(issuer: "evil-issuer") },
            { "expired beyond clock skew", Token(expires: DateTime.UtcNow.AddMinutes(-5)) },
            { "alg none", $"{unsignedHeader}.{parts[1]}." },
            { "payload edited after signing", $"{parts[0]}.{Base64UrlEncoder.Encode("""{"sub":"2","iss":"taskflow-api","aud":"taskflow-web"}""")}.{parts[2]}" },
            { "garbage", "not-a-jwt" },
        };
    }

    [Theory]
    [MemberData(nameof(TamperedTokens))]
    public async Task TamperedOrInvalidToken_IsRejected(string why, string token)
    {
        Assert.True(await GetTasksWith(token) == HttpStatusCode.Unauthorized, $"{why} should be rejected");
    }

    [Fact]
    public async Task Login_IsRateLimited_WithRetryAfter()
    {
        var client = _factory.CreateClient();
        HttpResponseMessage? last = null;
        for (var i = 0; i < 11; i++)
        {
            last = await client.PostAsync("/api/auth/login", Json("""{"email":"limit@company.com","password":"wrong-password-1"}"""));
        }

        Assert.Equal(HttpStatusCode.TooManyRequests, last!.StatusCode);
        Assert.True(int.Parse(last.Headers.GetValues("Retry-After").Single()) > 0);
        var body = JsonDocument.Parse(await last.Content.ReadAsStringAsync()).RootElement;
        Assert.Equal("RATE_LIMITED", body.GetProperty("errorCode").GetString());
    }

    [Fact]
    public async Task Users_RequiresAuth_AndListsRegisteredDisplayNames()
    {
        var client = _factory.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/users")).StatusCode);

        var register = await client.PostAsync("/api/auth/register",
            Json($$"""{"displayName":"Zara Quinn","email":"zara{{Guid.NewGuid():N}}@company.com","password":"harbour-lantern-47"}"""));
        var token = JsonDocument.Parse(await register.Content.ReadAsStringAsync()).RootElement.GetProperty("accessToken").GetString();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var users = JsonDocument.Parse(await client.GetStringAsync("/api/users")).RootElement;
        Assert.Contains(users.EnumerateArray(), u => u.GetProperty("displayName").GetString() == "Zara Quinn");
        Assert.All(users.EnumerateArray(), u => Assert.False(u.TryGetProperty("email", out _))); // names only
    }
}
