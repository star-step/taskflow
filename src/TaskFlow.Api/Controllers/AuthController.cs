using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.JsonWebTokens;
using TaskFlow.Api.Infrastructure;
using TaskFlow.Api.Services;
using TaskFlow.Shared.Contracts.Auth;

namespace TaskFlow.Api.Controllers;

[ApiController]
[Route("api/auth")]
[Produces("application/json")]
public sealed class AuthController : ControllerBase
{
    private readonly IAuthService _auth;
    private readonly RefreshCookie _cookie;

    public AuthController(IAuthService auth, RefreshCookie cookie)
    {
        _auth = auth;
        _cookie = cookie;
    }

    /// <summary>Creates an account and signs it in. Sets the refresh cookie.</summary>
    [HttpPost("register")]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimits.Register)]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AuthResponse>> Register(RegisterRequest request, CancellationToken ct)
    {
        var result = await _auth.RegisterAsync(request, ct);
        _cookie.Set(Response, result.RefreshToken, result.RefreshExpiresAt);
        return CreatedAtAction(nameof(Me), result.Response);
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimits.Login)]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request, CancellationToken ct)
    {
        var result = await _auth.LoginAsync(request, ct);
        _cookie.Set(Response, result.RefreshToken, result.RefreshExpiresAt);
        return Ok(result.Response);
    }

    /// <summary>Exchanges the refresh cookie for a new access token and rotates the cookie.</summary>
    [HttpPost("refresh")]
    [AllowAnonymous]
    [RequireClientHeader]
    [EnableRateLimiting(RateLimits.Refresh)]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponse>> Refresh(CancellationToken ct)
    {
        try
        {
            var result = await _auth.RefreshAsync(_cookie.Read(Request), ct);
            _cookie.Set(Response, result.RefreshToken, result.RefreshExpiresAt);
            return Ok(result.Response);
        }
        catch
        {
            _cookie.Clear(Response);
            throw;
        }
    }

    [HttpPost("logout")]
    [AllowAnonymous]
    [RequireClientHeader]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Logout(CancellationToken ct)
    {
        await _auth.LogoutAsync(_cookie.Read(Request), ct);
        _cookie.Clear(Response);
        return NoContent();
    }

    [HttpGet("me")]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UserResponse>> Me(CancellationToken ct)
    {
        var id = int.Parse(User.FindFirstValue(JwtRegisteredClaimNames.Sub)!);
        return Ok(await _auth.GetUserAsync(id, ct));
    }
}
