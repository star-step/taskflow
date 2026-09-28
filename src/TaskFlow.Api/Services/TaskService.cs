using Microsoft.EntityFrameworkCore;
using TaskFlow.Api.Data;
using TaskFlow.Api.Domain;
using TaskFlow.Api.Errors;
using TaskFlow.Api.Mapping;
using TaskFlow.Shared.Contracts.Tasks;
using TaskFlow.Shared.Enums;

namespace TaskFlow.Api.Services;

public sealed class TaskService : ITaskService
{
    private readonly AppDbContext _db;

    public TaskService(AppDbContext db) => _db = db;

    /// <summary>
    /// Builds one SQL query from optional filters and a sort order. The soft-delete filter is applied
    /// globally by the DbContext; list filters become IN (...), and the projection runs in SQL.
    /// </summary>
    public async Task<IReadOnlyList<TaskResponse>> GetTasksAsync(TaskQuery q, CancellationToken ct = default)
    {
        IQueryable<TaskItem> tasks = _db.Tasks.AsNoTracking();

        if (q.Statuses.Count > 0)
        {
            var statuses = q.Statuses.Distinct().ToList();
            tasks = tasks.Where(t => statuses.Contains(t.Status));
        }

        if (q.Priorities.Count > 0)
        {
            var priorities = q.Priorities.Distinct().ToList();
            tasks = tasks.Where(t => priorities.Contains(t.Priority));
        }

        if (!string.IsNullOrWhiteSpace(q.AssignedTo))
        {
            var assignee = q.AssignedTo.Trim();
            tasks = tasks.Where(t => t.AssignedTo != null && t.AssignedTo.Contains(assignee));
        }

        if (!string.IsNullOrWhiteSpace(q.Search))
        {
            var term = q.Search.Trim();
            tasks = tasks.Where(t => t.Title.Contains(term) || (t.Description != null && t.Description.Contains(term)));
        }

        var desc = q.SortDir == SortDirection.Desc;
        IOrderedQueryable<TaskItem> ordered = q.SortBy switch
        {
            TaskSortField.Modified => desc ? tasks.OrderByDescending(t => t.ModifiedDate) : tasks.OrderBy(t => t.ModifiedDate),
            TaskSortField.Created => desc ? tasks.OrderByDescending(t => t.CreatedDate) : tasks.OrderBy(t => t.CreatedDate),
            TaskSortField.Title => desc ? tasks.OrderByDescending(t => t.Title) : tasks.OrderBy(t => t.Title),
            TaskSortField.Status => desc ? tasks.OrderByDescending(t => t.Status) : tasks.OrderBy(t => t.Status),
            _ => desc ? tasks.OrderByDescending(t => t.Priority) : tasks.OrderBy(t => t.Priority)
        };

        // Secondary order keeps results stable: most recently touched first, then by id.
        var sorted = ordered.ThenByDescending(t => t.ModifiedDate).ThenBy(t => t.Id);

        return await sorted.Select(TaskMappings.ToResponse).ToListAsync(ct);
    }

    public async Task<TaskResponse> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var task = await _db.Tasks.AsNoTracking()
            .Where(t => t.Id == id)
            .Select(TaskMappings.ToResponse)
            .FirstOrDefaultAsync(ct);
        return task ?? throw new NotFoundException("Task", id);
    }

    public async Task<TaskResponse> CreateAsync(CreateTaskRequest request, CancellationToken ct = default)
    {
        var task = new TaskItem
        {
            Title = RequireTitle(request.Title),
            Description = Clean(request.Description),
            Status = request.Status ?? TaskItemStatus.ToDo,
            Priority = request.Priority ?? throw new DomainValidationException("priority", "Priority is required."),
            AssignedTo = Clean(request.AssignedTo)
        };

        _db.Tasks.Add(task);
        await _db.SaveChangesAsync(ct);
        return task.ToDto();
    }

    public async Task<TaskResponse> UpdateAsync(int id, UpdateTaskRequest request, CancellationToken ct = default)
    {
        var task = await FindAsync(id, ct);
        task.Title = RequireTitle(request.Title);
        task.Description = Clean(request.Description);
        task.Status = request.Status ?? throw new DomainValidationException("status", "Status is required.");
        task.Priority = request.Priority ?? throw new DomainValidationException("priority", "Priority is required.");
        task.AssignedTo = Clean(request.AssignedTo);

        await _db.SaveChangesAsync(ct);
        return task.ToDto();
    }

    public async Task<TaskResponse> UpdateStatusAsync(int id, TaskItemStatus status, CancellationToken ct = default)
    {
        if (!Enum.IsDefined(status))
        {
            throw new DomainValidationException("status", "Status must be one of: ToDo, InProgress, Done.");
        }

        var task = await FindAsync(id, ct);
        if (task.Status != status)
        {
            task.Status = status;
            await _db.SaveChangesAsync(ct);
        }

        return task.ToDto();
    }

    public async Task SoftDeleteAsync(int id, CancellationToken ct = default)
    {
        var task = await FindAsync(id, ct);
        task.IsDeleted = true;
        await _db.SaveChangesAsync(ct);
    }

    private async Task<TaskItem> FindAsync(int id, CancellationToken ct) =>
        await _db.Tasks.FirstOrDefaultAsync(t => t.Id == id, ct) ?? throw new NotFoundException("Task", id);

    private static string RequireTitle(string? title)
    {
        var trimmed = title?.Trim();
        if (string.IsNullOrEmpty(trimmed))
        {
            throw new DomainValidationException("title", "Title is required.");
        }

        return trimmed;
    }

    private static string? Clean(string? value)
    {
        var trimmed = value?.Trim();
        return string.IsNullOrEmpty(trimmed) ? null : trimmed;
    }
}
