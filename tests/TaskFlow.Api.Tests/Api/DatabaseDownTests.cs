using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace TaskFlow.Api.Tests.Api;

/// <summary>
/// The real API pointed at a SQL Server address where nothing is listening: an outage must read as
/// 503 DATABASE_UNAVAILABLE (not a 500 bug), and must not sign the user out.
/// </summary>
public sealed class DatabaseDownTests : IClassFixture<DatabaseDownTests.DownFactory>
{
    private const string Key = "db-down-test-signing-key-0123456789abcdef";
    private readonly DownFactory _factory;

    public DatabaseDownTests(DownFactory factory) => _factory = factory;

    public sealed class DownFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.UseSetting("ConnectionStrings:TaskFlow",
                "Server=127.0.0.1,1;Database=TaskFlow;User Id=sa;Password=x;TrustServerCertificate=True;Connect Timeout=2;ConnectRetryCount=0");
            builder.UseSetting("Jwt:SigningKey", Key);
        }
    }

    private HttpClient AuthorisedClient()
    {
        var token = new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Issuer = "taskflow-api",
            Audience = "taskflow-web",
            Expires = DateTime.UtcNow.AddMinutes(10),
            Subject = new ClaimsIdentity(new[] { new Claim("sub", "1") }),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Key)), SecurityAlgorithms.HmacSha256)
        });
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    [Theory]
    [InlineData("/api/tasks")]          // EF Core
    [InlineData("/api/tasks/summary")]  // raw SqlClient
    public async Task UnreachableDatabase_Is503_NotA500(string path)
    {
        var response = await AuthorisedClient().GetAsync(path);

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
        Assert.Equal("DATABASE_UNAVAILABLE", body.GetProperty("errorCode").GetString());
    }

    [Fact]
    public async Task RefreshDuringAnOutage_KeepsTheCookie()
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/refresh");
        request.Headers.Add("X-TaskFlow-Client", "web");
        request.Headers.Add("Cookie", "tf_refresh=some-token");

        var response = await _factory.CreateClient(new() { HandleCookies = false }).SendAsync(request);

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.False(response.Headers.TryGetValues("Set-Cookie", out _), "an outage must not sign the user out");
    }
}
