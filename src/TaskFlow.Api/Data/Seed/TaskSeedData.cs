using TaskFlow.Api.Domain;
using TaskFlow.Shared.Enums;

namespace TaskFlow.Api.Data.Seed;

/// <summary>
/// Sample tasks inserted by the SeedSampleTasks migration. HasData needs fixed values, so dates are
/// literals. Row 13 is soft-deleted to demonstrate that it never appears in lists or the summary.
/// Visible totals: To Do 5, In Progress 4, Done 3; Critical 3, High 4, Medium 2, Low 3.
/// </summary>
internal static class TaskSeedData
{
    public static IReadOnlyList<TaskItem> Tasks { get; } = new[]
    {
        Task(1, "Set up CI pipeline for API", "Build, test and publish the API image on every pull request.",
            TaskItemStatus.InProgress, TaskPriority.High, "Priya Nair", At(18, 10, 0), At(28, 10, 0)),
        Task(2, "Migrate user reports to new schema", "Move ReportRuns into the reporting schema; keep a view for old queries.",
            TaskItemStatus.ToDo, TaskPriority.Critical, "Daniel Okafor", At(15, 14, 20), At(26, 12, 0)),
        Task(3, "Fix date picker timezone offset", "Due dates shift by one day for users in UTC−5 and further west.",
            TaskItemStatus.InProgress, TaskPriority.Critical, "Marta Silva", At(24, 9, 5), At(28, 11, 25)),
        Task(4, "Write onboarding guide for new hires", "Cover local setup, Docker, and how we triage tasks.",
            TaskItemStatus.ToDo, TaskPriority.Low, "Sam Chen", At(10, 11, 0), At(25, 12, 0)),
        Task(5, "Review Q3 access permissions audit", "Confirm leavers no longer have access to production resources.",
            TaskItemStatus.Done, TaskPriority.High, "Aisha Rahman", At(8, 8, 30), At(24, 12, 0)),
        Task(6, "Add rate limiting to public endpoints", "100 requests/min per key; return 429 with Retry-After.",
            TaskItemStatus.ToDo, TaskPriority.High, "Daniel Okafor", At(22, 13, 10), At(28, 7, 0)),
        Task(7, "Update npm dependencies after audit", "Resolve the three high-severity advisories from npm audit.",
            TaskItemStatus.Done, TaskPriority.Medium, "Sam Chen", At(14, 15, 45), At(22, 12, 0)),
        Task(8, "Design empty states for dashboard", "No tasks, no results and no-permission variants.",
            TaskItemStatus.InProgress, TaskPriority.Medium, "Lena Fischer", At(21, 10, 30), At(28, 11, 0)),
        Task(9, "Archive legacy SharePoint site", "Export to the records archive, then set the site to read-only.",
            TaskItemStatus.ToDo, TaskPriority.Low, "Aisha Rahman", At(1, 9, 0), At(14, 12, 0)),
        Task(10, "Investigate slow task list query", "List endpoint takes 1.8 s with 5k rows; check for a missing index.",
            TaskItemStatus.InProgress, TaskPriority.High, "Priya Nair", At(25, 16, 0), At(28, 11, 40)),
        Task(11, "Prepare sprint demo slides", "Five slides: shipped, in progress, blocked.",
            TaskItemStatus.Done, TaskPriority.Low, "Lena Fischer", At(19, 9, 30), At(23, 12, 0)),
        Task(12, "Rotate SQL Server service credentials", "Quarterly rotation; update the Key Vault secret and restart the API.",
            TaskItemStatus.ToDo, TaskPriority.Critical, "Marta Silva", At(21, 9, 15), At(27, 16, 40)),
        Task(13, "Evaluate Trello export", "Superseded by this tool.",
            TaskItemStatus.ToDo, TaskPriority.Low, "Sam Chen", At(2, 10, 0), At(12, 10, 0), isDeleted: true),
    };

    private static DateTime At(int day, int hour, int minute) =>
        new(2026, 9, day, hour, minute, 0, DateTimeKind.Utc);

    private static TaskItem Task(int id, string title, string description, TaskItemStatus status,
        TaskPriority priority, string assignedTo, DateTime created, DateTime modified, bool isDeleted = false) => new()
    {
        Id = id,
        Title = title,
        Description = description,
        Status = status,
        Priority = priority,
        AssignedTo = assignedTo,
        CreatedDate = created,
        ModifiedDate = modified,
        IsDeleted = isDeleted
    };
}
