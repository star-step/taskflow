using System.Collections.Generic;
using TaskFlow.Shared.Enums;

namespace TaskFlow.Shared.Contracts.Tasks;

/// <summary>Counts of non-deleted tasks by status, by priority, and by each status/priority pair.</summary>
public class TaskSummaryResponse
{
    public int Total { get; set; }

    /// <summary>Every status is present, with 0 where there are no tasks.</summary>
    public Dictionary<TaskItemStatus, int> ByStatus { get; set; } = new();

    /// <summary>Every priority is present, with 0 where there are no tasks.</summary>
    public Dictionary<TaskPriority, int> ByPriority { get; set; } = new();

    /// <summary>One entry per status/priority pair that has at least one task.</summary>
    public List<TaskSummaryCell> Matrix { get; set; } = new();
}

public class TaskSummaryCell
{
    public TaskItemStatus Status { get; set; }
    public TaskPriority Priority { get; set; }
    public int Count { get; set; }
}
