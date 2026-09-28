using System.Linq.Expressions;
using TaskFlow.Api.Domain;
using TaskFlow.Shared.Contracts.Tasks;

namespace TaskFlow.Api.Mapping;

public static class TaskMappings
{
    /// <summary>Projection used inside EF queries, so only the needed columns are read.</summary>
    public static readonly Expression<Func<TaskItem, TaskResponse>> ToResponse = t => new TaskResponse
    {
        Id = t.Id,
        Title = t.Title,
        Description = t.Description,
        Status = t.Status,
        Priority = t.Priority,
        AssignedTo = t.AssignedTo,
        CreatedDate = t.CreatedDate,
        ModifiedDate = t.ModifiedDate
    };

    private static readonly Func<TaskItem, TaskResponse> Compiled = ToResponse.Compile();

    public static TaskResponse ToDto(this TaskItem task) => Compiled(task);
}
