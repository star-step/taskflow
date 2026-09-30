using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TaskFlow.Api.Data;

namespace TaskFlow.Api.Tests.Support;

/// <summary>
/// The real API (Program, middleware, MVC, auth) with the database swapped for in-memory SQLite.
/// Catches wiring problems unit tests can't, such as options registered in the wrong order.
/// </summary>
public sealed class ApiFactory : WebApplicationFactory<Program>
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");

    /// <summary>Every log entry written by the API during the tests.</summary>
    public CapturingLoggerProvider Logs { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        _connection.Open();
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:TaskFlow", "Server=unused-in-tests");
        builder.UseSetting("Jwt:SigningKey", "pipeline-test-signing-key-0123456789abcdef");
        builder.UseSetting("Auth:AllowedEmailDomains:0", "company.com");
        builder.UseSetting("Auth:CookieSecure", "false");
        builder.UseSetting("RateLimits:RegisterPerTenMinutes", "100");
        // Let tests see the Debug-level health-probe lines (hidden by the default Information minimum).
        builder.UseSetting("Logging:LogLevel:TaskFlow.Api.Infrastructure.RequestLoggingMiddleware", "Debug");

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.AddDbContext<AppDbContext>(o => o.UseSqlite(_connection));
            services.AddSingleton<ILoggerProvider>(Logs);
        });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);
        using var scope = host.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.EnsureCreated();
        return host;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        _connection.Dispose();
    }
}
