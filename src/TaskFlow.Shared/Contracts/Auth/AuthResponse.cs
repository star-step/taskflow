using System;

namespace TaskFlow.Shared.Contracts.Auth;

public class AuthResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public UserResponse User { get; set; } = new();
}

public class UserResponse
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
}

/// <summary>Minimal user info for assignee suggestions.</summary>
public class UserSummary
{
    public int Id { get; set; }
    public string DisplayName { get; set; } = string.Empty;
}
