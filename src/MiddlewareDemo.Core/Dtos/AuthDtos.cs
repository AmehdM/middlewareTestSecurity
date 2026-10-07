namespace MiddlewareDemo.Core.Dtos;

public sealed record LoginRequest(string Email, string Password, bool RememberMe = false);

public sealed record RegisterRequest(string Email, string Password, string DisplayName, string NationalId);

public sealed record ConfirmResetRequest(string Email, string Token, string NewPassword);

public sealed record TokenPair(string AccessToken, string RefreshToken, DateTime ExpiresAt);

public sealed record UserProfileDto(
    int Id,
    string Email,
    string DisplayName,
    string Role,
    bool IsAdmin,
    string NationalId,
    string PasswordHash,
    string TenantId);

public sealed record AuthResult(UserProfileDto User, TokenPair Tokens, string? RememberToken);

public sealed record ApiClientCredentials(string ClientId, string ClientSecret);
