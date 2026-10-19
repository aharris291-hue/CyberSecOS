namespace CyberSecOS.Core.Models;

public sealed record AuthStatus(bool Registered);

public sealed record RegisterRequest(string Username, string Password, string DisplayName);

public sealed record LoginRequest(string Username, string Password);

public sealed record UserProfile(
    int Id,
    string Username,
    string DisplayName,
    string TargetRole,
    int WeeklyGoalHours,
    DateTime CreatedAt,
    DateTime? LastLoginAt);

public sealed record TokenResponse(string AccessToken, string TokenType, int ExpiresIn, UserProfile User);

public sealed record ProfileUpdate(string? DisplayName = null, string? TargetRole = null, int? WeeklyGoalHours = null);

public sealed record PasswordChange(string CurrentPassword, string NewPassword);
