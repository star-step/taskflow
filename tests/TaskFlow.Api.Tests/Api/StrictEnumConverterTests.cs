using System.Text.Json;
using TaskFlow.Api.Infrastructure;
using TaskFlow.Shared.Contracts.Tasks;
using TaskFlow.Shared.Enums;

namespace TaskFlow.Api.Tests.Api;

public sealed class StrictEnumConverterTests
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new StrictEnumConverterFactory() }
    };

    [Fact]
    public void DictionaryKeys_SerializeAsNames_AndRoundTrip()
    {
        // The summary endpoint returns Dictionary<TaskItemStatus, int>, which needs property-name support.
        var summary = new TaskSummaryResponse { ByStatus = { [TaskItemStatus.InProgress] = 4 } };

        var json = JsonSerializer.Serialize(summary, Options);
        var back = JsonSerializer.Deserialize<TaskSummaryResponse>(json, Options)!;

        Assert.Contains("\"InProgress\":4", json);
        Assert.Equal(4, back.ByStatus[TaskItemStatus.InProgress]);
    }

    [Theory]
    [InlineData("\"Medium, High\"")]
    [InlineData("\"Low,Critical\"")]
    [InlineData("3")]
    [InlineData("\"3\"")]
    [InlineData("\"Urgent\"")]
    [InlineData("\"\"")]
    public void Read_RejectsAnythingButOneName(string json) =>
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<TaskPriority>(json, Options));

    [Fact]
    public void Nullable_AllowsNull() =>
        Assert.Null(JsonSerializer.Deserialize<TaskPriority?>("null", Options));
}
