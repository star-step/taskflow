using TaskFlow.Shared.Contracts.Tasks;

namespace TaskFlow.Api.Services;

public interface ITaskSummaryQuery
{
    Task<TaskSummaryResponse> GetSummaryAsync(CancellationToken ct = default);
}
