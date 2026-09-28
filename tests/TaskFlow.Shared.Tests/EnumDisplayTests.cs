using TaskFlow.Shared.Enums;
using TaskFlow.Shared.Utilities;

namespace TaskFlow.Shared.Tests;

public sealed class EnumDisplayTests
{
    [Theory]
    [InlineData(TaskItemStatus.ToDo, "To Do")]
    [InlineData(TaskItemStatus.InProgress, "In Progress")]
    [InlineData(TaskItemStatus.Done, "Done")]
    public void ToDisplayName_IsHumanReadable(TaskItemStatus status, string expected) =>
        Assert.Equal(expected, status.ToDisplayName());

    [Theory]
    [InlineData("in progress", TaskItemStatus.InProgress)]
    [InlineData("IN_PROGRESS", TaskItemStatus.InProgress)]
    [InlineData("to-do", TaskItemStatus.ToDo)]
    [InlineData("done", TaskItemStatus.Done)]
    public void TryParseFlexible_AcceptsCommonSpellings(string input, TaskItemStatus expected)
    {
        Assert.True(EnumDisplay.TryParseFlexible<TaskItemStatus>(input, out var value));
        Assert.Equal(expected, value);
    }

    [Theory]
    [InlineData("1")]
    [InlineData("99")]
    [InlineData("ToDo,Done")]
    [InlineData("urgent")]
    [InlineData("")]
    [InlineData(null)]
    public void TryParseFlexible_RejectsNumbersListsAndUnknownNames(string? input) =>
        Assert.False(EnumDisplay.TryParseFlexible<TaskItemStatus>(input, out _));

    [Fact]
    public void EveryMember_RoundTripsThroughDisplayName()
    {
        foreach (var status in Enum.GetValues<TaskItemStatus>())
        {
            Assert.True(EnumDisplay.TryParseFlexible<TaskItemStatus>(status.ToDisplayName(), out var parsed));
            Assert.Equal(status, parsed);
        }

        foreach (var priority in Enum.GetValues<TaskPriority>())
        {
            Assert.True(EnumDisplay.TryParseFlexible<TaskPriority>(priority.ToDisplayName(), out var parsed));
            Assert.Equal(priority, parsed);
        }
    }
}
