using System.ComponentModel.DataAnnotations;
using TaskFlow.Shared.Enums;
using TaskFlow.Shared.Validation;

namespace TaskFlow.Shared.Contracts.Tasks;

/// <summary>Status-only change (PATCH), used by the inline dropdown and the board.</summary>
public class UpdateTaskStatusRequest
{
    [Required(ErrorMessage = "Status is required.")]
    [DefinedEnum]
    public TaskItemStatus? Status { get; set; }
}
