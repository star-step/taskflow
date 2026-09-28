using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TaskFlow.Api.Data;
using TaskFlow.Api.Errors;
using TaskFlow.Api.Services;
using TaskFlow.Api.Tests.Support;
using TaskFlow.Shared.Contracts.Tasks;
using TaskFlow.Shared.Enums;

namespace TaskFlow.Api.Tests.Services;

/// <summary>
/// TaskService against the seeded database: 12 visible tasks plus task 13, which is soft-deleted.
/// </summary>
public sealed class TaskServiceTests : IAsyncLifetime
{
    private readonly TestHost _host = new();
    private AsyncServiceScope _scope;
    private ITaskService _service = null!;

    public Task InitializeAsync()
    {
        _scope = _host.Scope();
        _service = _scope.ServiceProvider.GetRequiredService<ITaskService>();
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        await _scope.DisposeAsync();
        await _host.DisposeAsync();
    }

    [Fact]
    public async Task GetTasksAsync_FiltersByStatusAndPriority()
    {
        var result = await _service.GetTasksAsync(new TaskQuery
        {
            Statuses = new[] { TaskItemStatus.InProgress },
            Priorities = new[] { TaskPriority.High, TaskPriority.Critical }
        });

        Assert.Equal(new[] { 3, 10, 1 }, result.Select(t => t.Id));
        Assert.All(result, t => Assert.Equal(TaskItemStatus.InProgress, t.Status));
        Assert.All(result, t => Assert.Contains(t.Priority, new[] { TaskPriority.High, TaskPriority.Critical }));
    }

    [Fact]
    public async Task GetTasksAsync_ExcludesSoftDeleted()
    {
        var all = await _service.GetTasksAsync(new TaskQuery());

        Assert.Equal(12, all.Count);
        Assert.DoesNotContain(all, t => t.Id == 13);
    }

    [Fact]
    public async Task GetTasksAsync_DefaultSort_IsCriticalFirstThenMostRecentlyModified()
    {
        var result = await _service.GetTasksAsync(new TaskQuery());

        // Same order as the mockups: Critical (3, 12, 2), High (10, 1, 6, 5), Medium (8, 7), Low (4, 11, 9).
        Assert.Equal(new[] { 3, 12, 2, 10, 1, 6, 5, 8, 7, 4, 11, 9 }, result.Select(t => t.Id));
    }

    [Fact]
    public async Task GetTasksAsync_SearchMatchesTitleOrDescription_AndFiltersAssignee()
    {
        var bySearch = await _service.GetTasksAsync(new TaskQuery { Search = "schema" });
        var byAssignee = await _service.GetTasksAsync(new TaskQuery { AssignedTo = "Priya" });

        Assert.Equal(new[] { 2 }, bySearch.Select(t => t.Id));
        Assert.Equal(new[] { 10, 1 }, byAssignee.Select(t => t.Id));
    }

    [Fact]
    public async Task CreateAsync_SetsCreatedAndModifiedDates_AndTrimsFields()
    {
        var created = await _service.CreateAsync(new CreateTaskRequest
        {
            Title = "  Write release notes  ",
            Description = "   ",
            Priority = TaskPriority.Medium,
            AssignedTo = " Sam Chen "
        });

        Assert.Equal("Write release notes", created.Title);
        Assert.Null(created.Description);
        Assert.Equal("Sam Chen", created.AssignedTo);
        Assert.Equal(TaskItemStatus.ToDo, created.Status);
        Assert.Equal(TestHost.Start.UtcDateTime, created.CreatedDate);
        Assert.Equal(created.CreatedDate, created.ModifiedDate);
        Assert.Equal(DateTimeKind.Utc, created.CreatedDate.Kind);
    }

    [Fact]
    public async Task CreateAsync_WhitespaceTitle_ThrowsDomainValidationException()
    {
        var ex = await Assert.ThrowsAsync<DomainValidationException>(() => _service.CreateAsync(new CreateTaskRequest
        {
            Title = "   ",
            Priority = TaskPriority.Low
        }));

        Assert.Equal(new[] { "Title is required." }, ex.Errors!["title"]);
    }

    [Fact]
    public async Task UpdateStatusAsync_UpdatesModifiedDate_NotCreatedDate()
    {
        var before = await _service.GetByIdAsync(4);
        _host.Clock.Advance(TimeSpan.FromHours(3));

        var after = await _service.UpdateStatusAsync(4, TaskItemStatus.InProgress);

        Assert.Equal(TaskItemStatus.InProgress, after.Status);
        Assert.Equal(before.CreatedDate, after.CreatedDate);
        Assert.Equal(TestHost.Start.UtcDateTime.AddHours(3), after.ModifiedDate);

        var reloaded = await _service.GetByIdAsync(4);
        Assert.Equal(before.CreatedDate, reloaded.CreatedDate);
        Assert.Equal(after.ModifiedDate, reloaded.ModifiedDate);
    }

    [Fact]
    public async Task UpdateAsync_UnknownId_ThrowsNotFound()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => _service.UpdateAsync(999, new UpdateTaskRequest
        {
            Title = "Anything",
            Status = TaskItemStatus.Done,
            Priority = TaskPriority.Low
        }));
    }

    [Fact]
    public async Task SoftDeleteAsync_SetsIsDeleted_RowStillExists()
    {
        await _service.SoftDeleteAsync(9);

        await Assert.ThrowsAsync<NotFoundException>(() => _service.GetByIdAsync(9));
        await Assert.ThrowsAsync<NotFoundException>(() => _service.SoftDeleteAsync(9));

        await using var scope = _host.Scope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var row = await db.Tasks.IgnoreQueryFilters().SingleAsync(t => t.Id == 9);
        Assert.True(row.IsDeleted);
        Assert.Equal(TestHost.Start.UtcDateTime, row.ModifiedDate);
    }
}
