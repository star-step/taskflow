using System.ComponentModel.DataAnnotations;
using TaskFlow.Shared.Enums;
using TaskFlow.Shared.Validation;

namespace TaskFlow.Shared.Contracts.Tasks;

public class CreateTaskRequest
{
    [Required(ErrorMessage = "Title is required.")]
    [NotWhiteSpace(ErrorMessage = "Title is required.")]
    [StringLength(TaskFieldLimits.TitleMaxLength, ErrorMessage = "Title must be {1} characters or fewer.")]
    public string Title { get; set; } = string.Empty;

    [StringLength(TaskFieldLimits.DescriptionMaxLength, ErrorMessage = "Description must be {1} characters or fewer.")]
    public string? Description { get; set; }

    /// <summary>Optional; defaults to <see cref="TaskItemStatus.ToDo"/>.</summary>
    [DefinedEnum]
    public TaskItemStatus? Status { get; set; }

    [Required(ErrorMessage = "Priority is required.")]
    [DefinedEnum]
    public TaskPriority? Priority { get; set; }

    [StringLength(TaskFieldLimits.AssignedToMaxLength, ErrorMessage = "Assigned to must be {1} characters or fewer.")]
    public string? AssignedTo { get; set; }
}
