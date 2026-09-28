namespace TaskFlow.Shared.Validation;

/// <summary>
/// Field lengths shared by request validation and the database column configuration.
/// </summary>
public static class TaskFieldLimits
{
    public const int TitleMaxLength = 200;
    public const int DescriptionMaxLength = 2000;
    public const int AssignedToMaxLength = 100;
}
