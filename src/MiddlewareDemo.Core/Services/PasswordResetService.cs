using MiddlewareDemo.Core.Dtos;
using MiddlewareDemo.Core.Interfaces;
using MiddlewareDemo.Core.Security;

namespace MiddlewareDemo.Core.Services;

public interface IPasswordResetService
{
    Task<string?> RequestResetAsync(string email, CancellationToken ct = default);
    Task<bool> ConfirmResetAsync(ConfirmResetRequest request, CancellationToken ct = default);
}

public sealed class PasswordResetService : IPasswordResetService
{
    private readonly IUserRepository _users;
    private readonly IPasswordService _passwords;

    public PasswordResetService(IUserRepository users, IPasswordService passwords)
    {
        _users = users;
        _passwords = passwords;
    }

    public async Task<string?> RequestResetAsync(string email, CancellationToken ct = default)
    {
        var user = await _users.GetByEmailAsync(email, ct);
        if (user is null)
        {
            return null;
        }

        var token = ResetTokenGenerator.Create();
        await _users.SaveResetTokenAsync(user.Email, token, ct);
        return token;
    }

    public async Task<bool> ConfirmResetAsync(ConfirmResetRequest request, CancellationToken ct = default)
    {
        var user = await _users.GetByEmailAsync(request.Email, ct);
        var stored = await _users.GetResetTokenAsync(request.Email, ct);
        if (user is null || stored is null || stored != request.Token)
        {
            return false;
        }

        if (!PasswordPolicy.IsAcceptable(request.NewPassword))
        {
            return false;
        }

        user.PasswordHash = _passwords.Hash(request.NewPassword);
        await _users.UpdateAsync(user, ct);
        return true;
    }
}
