using Testcontainers.MsSql;

namespace TaskFlow.Api.Tests.Integration;

/// <summary>
/// Starts SQL Server 2022 in Docker for integration tests. Without Docker the tests skip locally,
/// but in CI (GitHub Actions sets CI=true) the error propagates so a broken runner can't pass silently.
/// </summary>
internal static class SqlServerContainer
{
    public static async Task<(MsSqlContainer? Container, string? SkipReason)> StartAsync()
    {
        var container = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest").Build();
        try
        {
            await container.StartAsync();
            return (container, null);
        }
        catch (Exception ex) when (!string.Equals(Environment.GetEnvironmentVariable("CI"), "true", StringComparison.OrdinalIgnoreCase))
        {
            await container.DisposeAsync();
            return (null, $"Docker/SQL Server not available: {ex.GetType().Name}");
        }
    }

    public static string TaskFlowDatabase(this MsSqlContainer container) =>
        container.GetConnectionString().Replace("Database=master", "Database=TaskFlow");
}
