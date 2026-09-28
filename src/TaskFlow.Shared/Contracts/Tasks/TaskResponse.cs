using System;
using TaskFlow.Shared.Enums;

namespace TaskFlow.Shared.Contracts.Tasks;

public class TaskResponse
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public TaskItemStatus Status { get; set; }
    public TaskPriority Priority { get; set; }
    public string? AssignedTo { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime ModifiedDate { get; set; }
}
