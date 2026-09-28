namespace TaskFlow.Shared.Enums;

/// <summary>
/// Task urgency. Values ascend with urgency, so ordering by value orders by priority.
/// </summary>
public enum TaskPriority
{
    Low = 0,
    Medium = 1,
    High = 2,
    Critical = 3
}
