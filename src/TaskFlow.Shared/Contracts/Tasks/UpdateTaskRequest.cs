using System.ComponentModel.DataAnnotations;
using TaskFlow.Shared.Enums;
using TaskFlow.Shared.Validation;

namespace TaskFlow.Shared.Contracts.Tasks;

/// <summary>Full replacement of a task's editable fields (PUT).</summary>
public class UpdateTaskRequest
{
    [Required(ErrorMessage = "Title is required.")]
    [NotWhiteSpace(ErrorMessage = "Title is required.")]
    [StringLength(TaskFieldLimits.TitleMaxLength, ErrorMessage = "Title must be 200 characters or fewer.")]
    public string Title { get; set; } = string.Empty;

    [StringLength(TaskFieldLimits.DescriptionMaxLength, ErrorMessage = "Description must be 2000 characters or fewer.")]
    public string? Description { get; set; }

    [Required(ErrorMessage = "Status is required.")]
    [DefinedEnum]
    public TaskItemStatus? Status { get; set; }

    [Required(ErrorMessage = "Priority is required.")]
    [DefinedEnum]
    public TaskPriority? Priority { get; set; }

    [StringLength(TaskFieldLimits.AssignedToMaxLength, ErrorMessage = "Assigned to must be 100 characters or fewer.")]
    public string? AssignedTo { get; set; }
}
