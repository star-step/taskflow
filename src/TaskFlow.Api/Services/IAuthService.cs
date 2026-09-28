using TaskFlow.Shared.Contracts.Auth;

namespace TaskFlow.Api.Services;

/// <summary>The response body plus the raw refresh token, which the controller puts in a cookie.</summary>
public sealed record AuthResult(AuthResponse Response, string RefreshToken, DateTime RefreshExpiresAt);

public interface IAuthService
{
    Task<AuthResult> RegisterAsync(RegisterRequest request, CancellationToken ct = default);
    Task<AuthResult> LoginAsync(LoginRequest request, CancellationToken ct = default);
    Task<AuthResult> RefreshAsync(string? refreshToken, CancellationToken ct = default);
    Task LogoutAsync(string? refreshToken, CancellationToken ct = default);
    Task<UserResponse> GetUserAsync(int userId, CancellationToken ct = default);
}
