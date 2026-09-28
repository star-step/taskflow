using TaskFlow.Shared.Enums;

namespace TaskFlow.Api.Services;

public enum TaskSortField
{
    Priority,
    Modified,
    Created,
    Title,
    Status
}

public enum SortDirection
{
    Desc,
    Asc
}

/// <summary>Filters and sort order for listing tasks. Empty filter lists mean "any".</summary>
public sealed class TaskQuery
{
    public IReadOnlyCollection<TaskItemStatus> Statuses { get; init; } = Array.Empty<TaskItemStatus>();
    public IReadOnlyCollection<TaskPriority> Priorities { get; init; } = Array.Empty<TaskPriority>();
    public string? AssignedTo { get; init; }
    public string? Search { get; init; }
    public TaskSortField SortBy { get; init; } = TaskSortField.Priority;
    public SortDirection SortDir { get; init; } = SortDirection.Desc;
}
