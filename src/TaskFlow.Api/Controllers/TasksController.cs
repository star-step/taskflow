using Microsoft.AspNetCore.Mvc;
using TaskFlow.Api.Services;
using TaskFlow.Shared.Contracts.Tasks;

namespace TaskFlow.Api.Controllers;

[ApiController]
[Route("api/tasks")]
[Produces("application/json")]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
public sealed class TasksController : ControllerBase
{
    private readonly ITaskService _tasks;
    private readonly ITaskSummaryQuery _summary;

    public TasksController(ITaskService tasks, ITaskSummaryQuery summary)
    {
        _tasks = tasks;
        _summary = summary;
    }

    /// <summary>Lists tasks, optionally filtered by status, priority, assignee or search text.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<TaskResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyList<TaskResponse>>> List([FromQuery] TaskListQuery query, CancellationToken ct)
    {
        return Ok(await _tasks.GetTasksAsync(query.ToQuery(), ct));
    }

    /// <summary>Counts of tasks by status and priority, computed with a raw SQL query.</summary>
    [HttpGet("summary")]
    [ProducesResponseType(typeof(TaskSummaryResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<TaskSummaryResponse>> Summary(CancellationToken ct) =>
        Ok(await _summary.GetSummaryAsync(ct));

    [HttpGet("{id:int}", Name = nameof(Get))]
    [ProducesResponseType(typeof(TaskResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TaskResponse>> Get(int id, CancellationToken ct) =>
        Ok(await _tasks.GetByIdAsync(id, ct));

    [HttpPost]
    [ProducesResponseType(typeof(TaskResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<TaskResponse>> Create(CreateTaskRequest request, CancellationToken ct)
    {
        var created = await _tasks.CreateAsync(request, ct);
        return CreatedAtRoute(nameof(Get), new { id = created.Id }, created);
    }

    /// <summary>Replaces all editable fields of a task.</summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(TaskResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TaskResponse>> Update(int id, UpdateTaskRequest request, CancellationToken ct) =>
        Ok(await _tasks.UpdateAsync(id, request, ct));

    /// <summary>Changes only the status (inline dropdown, board drag).</summary>
    [HttpPatch("{id:int}/status")]
    [ProducesResponseType(typeof(TaskResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TaskResponse>> UpdateStatus(int id, UpdateTaskStatusRequest request, CancellationToken ct) =>
        Ok(await _tasks.UpdateStatusAsync(id, request.Status!.Value, ct));

    /// <summary>Soft-deletes a task: it disappears from every list but stays in the database.</summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await _tasks.SoftDeleteAsync(id, ct);
        return NoContent();
    }
}
