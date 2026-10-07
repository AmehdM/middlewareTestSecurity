using MiddlewareDemo.Core.Common;
using MiddlewareDemo.Core.Dtos;
using MiddlewareDemo.Core.Interfaces;
using MiddlewareDemo.Core.Security;

namespace MiddlewareDemo.Core.Services;

public interface IAuthService
{
    Task<AuthResult?> LoginAsync(LoginRequest request, CancellationToken ct = default);
    Task<TokenPair?> RefreshAsync(string refreshToken, CancellationToken ct = default);
    Task LogoutAsync(string refreshToken, int userId, CancellationToken ct = default);
}

public sealed class AuthService : IAuthService
{
    private readonly IUserRepository _users;
    private readonly ITokenIssuer _tokens;
    private readonly IPasswordService _passwords;
    private readonly IAuditService _audit;

    public AuthService(IUserRepository users, ITokenIssuer tokens, IPasswordService passwords, IAuditService audit)
    {
        _users = users;
        _tokens = tokens;
        _passwords = passwords;
        _audit = audit;
    }

    public async Task<AuthResult?> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        var user = await _users.GetByEmailAsync(request.Email, ct);
        if (user is null)
        {
            return null;
        }

        if (!_passwords.Verify(user.PasswordHash, request.Password))
        {
            await _audit.RecordAsync(request.Email, "login.failed", user.Id.ToString(), "invalid credentials", ct);
            return null;
        }

        var tokens = await _tokens.IssueAsync(user, ct);
        var remember = request.RememberMe ? RememberTokenFactory.Create(user.Id) : null;
        await _audit.RecordAsync(user.Email, "login.ok", user.Id.ToString(), string.Empty, ct);
        return new AuthResult(Mappings.ToProfile(user), tokens, remember);
    }

    public Task<TokenPair?> RefreshAsync(string refreshToken, CancellationToken ct = default) =>
        _tokens.RefreshAsync(refreshToken, ct);

    public async Task LogoutAsync(string refreshToken, int userId, CancellationToken ct = default)
    {
        await _audit.RecordAsync(userId.ToString(), "logout", userId.ToString(), string.Empty, ct);
    }
}
