using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Time.Testing;
using TaskFlow.Api.Data;
using TaskFlow.Api.Services;
using TaskFlow.Shared.Enums;
using Testcontainers.MsSql;

namespace TaskFlow.Api.Tests.Integration;

/// <summary>
/// Runs the raw GROUPING SETS summary against a real SQL Server in Docker, after applying the real
/// migrations. Skipped when Docker isn't available. Run only these with:
/// dotnet test --filter Category=Integration
/// </summary>
[Trait("Category", "Integration")]
public sealed class TaskSummaryQueryTests : IAsyncLifetime
{
    private MsSqlContainer? _sql;
    private string? _skipReason;

    public async Task InitializeAsync()
    {
        try
        {
            _sql = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest").Build();
            await _sql.StartAsync();
        }
        catch (Exception ex)
        {
            _skipReason = $"Docker/SQL Server not available: {ex.GetType().Name}";
        }
    }

    public async Task DisposeAsync()
    {
        if (_sql is not null)
        {
            await _sql.DisposeAsync();
        }
    }

    [SkippableFact]
    public async Task Summary_CountsSeededTasks_AndIgnoresSoftDeleted()
    {
        Skip.If(_skipReason is not null, _skipReason);

        var connectionString = _sql!.GetConnectionString().Replace("Database=master", "Database=TaskFlow");
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlServer(connectionString).Options;
        await using (var db = new AppDbContext(options, new FakeTimeProvider()))
        {
            await db.Database.MigrateAsync();
        }

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["ConnectionStrings:TaskFlow"] = connectionString })
            .Build();

        var summary = await new SqlTaskSummaryQuery(config).GetSummaryAsync();

        Assert.Equal(12, summary.Total); // row 13 is soft-deleted
        Assert.Equal(5, summary.ByStatus[TaskItemStatus.ToDo]);
        Assert.Equal(4, summary.ByStatus[TaskItemStatus.InProgress]);
        Assert.Equal(3, summary.ByStatus[TaskItemStatus.Done]);
        Assert.Equal(3, summary.ByPriority[TaskPriority.Critical]);
        Assert.Equal(4, summary.ByPriority[TaskPriority.High]);
        Assert.Equal(2, summary.ByPriority[TaskPriority.Medium]);
        Assert.Equal(3, summary.ByPriority[TaskPriority.Low]);
        Assert.Equal(12, summary.Matrix.Sum(c => c.Count));
        Assert.Contains(summary.Matrix, c => c is { Status: TaskItemStatus.ToDo, Priority: TaskPriority.Critical, Count: 2 });
    }
}
