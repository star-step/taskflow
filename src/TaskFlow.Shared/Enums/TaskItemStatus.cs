namespace TaskFlow.Shared.Enums;

/// <summary>
/// Workflow state of a task. Named to avoid clashing with System.Threading.Tasks.TaskStatus.
/// </summary>
public enum TaskItemStatus
{
    ToDo = 0,
    InProgress = 1,
    Done = 2
}
