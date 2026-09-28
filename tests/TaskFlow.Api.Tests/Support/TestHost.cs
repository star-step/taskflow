using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Time.Testing;
using TaskFlow.Api.Data;
using TaskFlow.Api.Infrastructure;
using TaskFlow.Api.Services;

namespace TaskFlow.Api.Tests.Support;

/// <summary>
/// Real service registrations over an in-memory SQLite database. SQLite is relational, so the
/// soft-delete query filter, IN (...) filters, ordering and unique indexes behave as they do on
/// SQL Server. Each instance gets its own database (seeded with the 13 migration rows) and a
/// frozen clock.
/// </summary>
public sealed class TestHost : IAsyncDisposable
{
    public static readonly DateTimeOffset Start = new(2026, 9, 28, 12, 0, 0, TimeSpan.Zero);

    private readonly SqliteConnection _connection;
    private readonly ServiceProvider _provider;

    public TestHost(Dictionary<string, string?>? settings = null)
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var values = new Dictionary<string, string?>
        {
            ["Jwt:Issuer"] = "taskflow-api",
            ["Jwt:Audience"] = "taskflow-web",
            ["Jwt:SigningKey"] = "test-signing-key-0123456789abcdef-0123456789",
            ["Auth:RefreshTokenDays"] = "7",
            ["Auth:Lockout:MaxFailedAttempts"] = "5",
            ["Auth:Lockout:Minutes"] = "15"
        };
        foreach (var (k, v) in settings ?? new())
        {
            values[k] = v;
        }

        var config = new ConfigurationBuilder().AddInMemoryCollection(values).Build();
        Clock = new FakeTimeProvider(Start);

        var services = new ServiceCollection();
        services.AddLogging(b => b.SetMinimumLevel(LogLevel.Warning));
        services.AddSingleton<TimeProvider>(Clock);
        services.AddSingleton<IConfiguration>(config);
        services.AddDbContext<AppDbContext>(o => o.UseSqlite(_connection));
        services.AddScoped<ITaskService, TaskService>();
        services.AddTaskFlowIdentity(config);
        _provider = services.BuildServiceProvider(validateScopes: true);

        using var scope = _provider.CreateScope();
        scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.EnsureCreated();
    }

    public FakeTimeProvider Clock { get; }

    /// <summary>A fresh DI scope, i.e. a new DbContext, like a new HTTP request.</summary>
    public AsyncServiceScope Scope() => _provider.CreateAsyncScope();

    public async ValueTask DisposeAsync()
    {
        await _provider.DisposeAsync();
        await _connection.DisposeAsync();
    }
}
