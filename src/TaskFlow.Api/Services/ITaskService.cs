using TaskFlow.Shared.Contracts.Tasks;
using TaskFlow.Shared.Enums;

namespace TaskFlow.Api.Services;

public interface ITaskService
{
    Task<IReadOnlyList<TaskResponse>> GetTasksAsync(TaskQuery query, CancellationToken ct = default);
    Task<TaskResponse> GetByIdAsync(int id, CancellationToken ct = default);
    Task<TaskResponse> CreateAsync(CreateTaskRequest request, CancellationToken ct = default);
    Task<TaskResponse> UpdateAsync(int id, UpdateTaskRequest request, CancellationToken ct = default);
    Task<TaskResponse> UpdateStatusAsync(int id, TaskItemStatus status, CancellationToken ct = default);
    Task SoftDeleteAsync(int id, CancellationToken ct = default);
}
