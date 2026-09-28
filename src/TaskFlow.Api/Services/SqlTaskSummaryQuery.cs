using Microsoft.Data.SqlClient;
using TaskFlow.Shared.Contracts.Tasks;
using TaskFlow.Shared.Enums;

namespace TaskFlow.Api.Services;

/// <summary>
/// Task counts by status and priority, using plain ADO.NET (SqlClient) rather than EF Core.
/// One GROUPING SETS query returns the status × priority matrix, the per-status and per-priority
/// totals, and the grand total. EF's soft-delete query filter doesn't apply to raw SQL, so the
/// query filters IsDeleted itself. The same SQL is in database/task-summary.sql.
/// </summary>
public sealed class SqlTaskSummaryQuery : ITaskSummaryQuery
{
    internal const string Sql = """
        SELECT
            t.[Status],
            t.[Priority],
            COUNT(*)               AS TaskCount,
            GROUPING(t.[Status])   AS IsStatusRollup,
            GROUPING(t.[Priority]) AS IsPriorityRollup
        FROM dbo.Tasks AS t
        WHERE t.IsDeleted = 0
        GROUP BY GROUPING SETS (
            (t.[Status], t.[Priority]),
            (t.[Status]),
            (t.[Priority]),
            ()
        )
        ORDER BY IsStatusRollup, IsPriorityRollup, t.[Status], t.[Priority];
        """;

    private readonly string _connectionString;

    public SqlTaskSummaryQuery(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("TaskFlow")
            ?? throw new InvalidOperationException("Connection string 'TaskFlow' is not configured.");
    }

    public async Task<TaskSummaryResponse> GetSummaryAsync(CancellationToken ct = default)
    {
        var summary = new TaskSummaryResponse
        {
            ByStatus = Enum.GetValues<TaskItemStatus>().ToDictionary(s => s, _ => 0),
            ByPriority = Enum.GetValues<TaskPriority>().ToDictionary(p => p, _ => 0)
        };

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(ct);
        await using var command = new SqlCommand(Sql, connection);
        await using var reader = await command.ExecuteReaderAsync(ct);

        var statusOrdinal = reader.GetOrdinal("Status");
        var priorityOrdinal = reader.GetOrdinal("Priority");
        var countOrdinal = reader.GetOrdinal("TaskCount");
        var statusRollupOrdinal = reader.GetOrdinal("IsStatusRollup");
        var priorityRollupOrdinal = reader.GetOrdinal("IsPriorityRollup");

        while (await reader.ReadAsync(ct))
        {
            var count = reader.GetInt32(countOrdinal);
            var statusRollup = Convert.ToInt32(reader.GetValue(statusRollupOrdinal)) == 1;
            var priorityRollup = Convert.ToInt32(reader.GetValue(priorityRollupOrdinal)) == 1;

            switch (statusRollup, priorityRollup)
            {
                case (false, false):
                    summary.Matrix.Add(new TaskSummaryCell
                    {
                        Status = (TaskItemStatus)reader.GetByte(statusOrdinal),
                        Priority = (TaskPriority)reader.GetByte(priorityOrdinal),
                        Count = count
                    });
                    break;
                case (false, true):
                    summary.ByStatus[(TaskItemStatus)reader.GetByte(statusOrdinal)] = count;
                    break;
                case (true, false):
                    summary.ByPriority[(TaskPriority)reader.GetByte(priorityOrdinal)] = count;
                    break;
                default:
                    summary.Total = count;
                    break;
            }
        }

        return summary;
    }
}
