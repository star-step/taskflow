using System.ComponentModel.DataAnnotations;
using TaskFlow.Api.Services;
using TaskFlow.Shared.Enums;

namespace TaskFlow.Api.Controllers;

/// <summary>Query string for GET /api/tasks. Repeat a parameter to match any of several values.</summary>
public sealed class TaskListQuery
{
    /// <summary>e.g. <c>?status=ToDo&amp;status=InProgress</c></summary>
    public TaskItemStatus[]? Status { get; set; }

    /// <summary>e.g. <c>?priority=High&amp;priority=Critical</c></summary>
    public TaskPriority[]? Priority { get; set; }

    /// <summary>Case-insensitive "contains" match on the assignee.</summary>
    [StringLength(100)]
    public string? AssignedTo { get; set; }

    /// <summary>Matches title or description.</summary>
    [StringLength(200)]
    public string? Search { get; set; }

    public TaskSortField SortBy { get; set; } = TaskSortField.Priority;
    public SortDirection SortDir { get; set; } = SortDirection.Desc;

    public TaskQuery ToQuery() => new()
    {
        Statuses = Status ?? Array.Empty<TaskItemStatus>(),
        Priorities = Priority ?? Array.Empty<TaskPriority>(),
        AssignedTo = AssignedTo,
        Search = Search,
        SortBy = SortBy,
        SortDir = SortDir
    };
}
