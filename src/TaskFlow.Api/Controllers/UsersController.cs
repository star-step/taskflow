using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskFlow.Api.Data;
using TaskFlow.Shared.Contracts.Auth;

namespace TaskFlow.Api.Controllers;

[ApiController]
[Route("api/users")]
[Produces("application/json")]
public sealed class UsersController : ControllerBase
{
    private readonly AppDbContext _db;

    public UsersController(AppDbContext db) => _db = db;

    /// <summary>Registered users' display names, used to suggest assignees.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<UserSummary>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<UserSummary>>> List(CancellationToken ct) =>
        Ok(await _db.Users.AsNoTracking()
            .OrderBy(u => u.DisplayName)
            .Select(u => new UserSummary { Id = u.Id, DisplayName = u.DisplayName })
            .ToListAsync(ct));
}
