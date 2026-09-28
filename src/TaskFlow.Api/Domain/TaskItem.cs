using TaskFlow.Shared.Enums;

namespace TaskFlow.Api.Domain;

public class TaskItem : IHasTimestamps
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public TaskItemStatus Status { get; set; } = TaskItemStatus.ToDo;
    public TaskPriority Priority { get; set; } = TaskPriority.Medium;
    public string? AssignedTo { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime ModifiedDate { get; set; }

    /// <summary>Soft-delete flag. Deleted tasks are hidden by a global query filter but stay in the table.</summary>
    public bool IsDeleted { get; set; }
}
