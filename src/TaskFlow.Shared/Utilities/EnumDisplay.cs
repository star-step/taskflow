using System;
using System.Linq;
using TaskFlow.Shared.Enums;

namespace TaskFlow.Shared.Utilities;

/// <summary>
/// Human-readable names for the task enums, and lenient parsing of user input such as "in progress".
/// </summary>
public static class EnumDisplay
{
    public static string ToDisplayName(this TaskItemStatus status) => status switch
    {
        TaskItemStatus.ToDo => "To Do",
        TaskItemStatus.InProgress => "In Progress",
        TaskItemStatus.Done => "Done",
        _ => status.ToString()
    };

    public static string ToDisplayName(this TaskPriority priority) => priority.ToString();

    /// <summary>
    /// Parses an enum member name, ignoring case, spaces, hyphens and underscores
    /// ("in progress", "IN_PROGRESS" and "inProgress" all match InProgress).
    /// Numeric strings are rejected so callers can't smuggle in undefined values.
    /// </summary>
    public static bool TryParseFlexible<TEnum>(string? input, out TEnum value) where TEnum : struct, Enum
    {
        value = default;
        if (string.IsNullOrWhiteSpace(input))
        {
            return false;
        }

        var compact = new string(input!.Where(c => !char.IsWhiteSpace(c) && c != '-' && c != '_').ToArray());
        // Enum.TryParse also accepts numbers and comma-separated flag lists ("ToDo,Done"); neither is a valid name.
        if (compact.Length == 0 || compact.Any(c => char.IsDigit(c) || c == ',' || c == '+'))
        {
            return false;
        }

        return Enum.TryParse(compact, ignoreCase: true, out value) && Enum.IsDefined(typeof(TEnum), value);
    }
}
