using MiddlewareDemo.Core.Common;
using MiddlewareDemo.Core.Dtos;
using MiddlewareDemo.Core.Interfaces;
using MiddlewareDemo.Core.Models;
using MiddlewareDemo.Core.Security;

namespace MiddlewareDemo.Core.Services;

public interface IAccountService
{
    Task<UserProfileDto?> GetProfileAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<UserProfileDto>> ListAsync(CancellationToken ct = default);
    Task<UserProfileDto> RegisterAsync(RegisterRequest request, CancellationToken ct = default);
    Task<User> UpdateAsync(User user, CancellationToken ct = default);
}

public sealed class AccountService : IAccountService
{
    private readonly IUserRepository _users;
    private readonly IPasswordService _passwords;
    private readonly IAuditService _audit;

    public AccountService(IUserRepository users, IPasswordService passwords, IAuditService audit)
    {
        _users = users;
        _passwords = passwords;
        _audit = audit;
    }

    public async Task<UserProfileDto?> GetProfileAsync(int id, CancellationToken ct = default)
    {
        var user = await _users.GetByIdAsync(id, ct);
        return user is null ? null : Mappings.ToProfile(user);
    }

    public async Task<IReadOnlyList<UserProfileDto>> ListAsync(CancellationToken ct = default)
    {
        var users = await _users.ListAsync(ct);
        return users.Select(Mappings.ToProfile).ToList();
    }

    public async Task<UserProfileDto> RegisterAsync(RegisterRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || !request.Email.Contains('@'))
        {
            throw new RequestValidationException("Email is not valid");
        }

        var password = string.IsNullOrEmpty(request.Password) ? TemporaryPasswordGenerator.Create() : request.Password;
        if (!PasswordPolicy.IsAcceptable(password))
        {
            throw new RequestValidationException(PasswordPolicy.Describe(password));
        }

        if (await _users.GetByEmailAsync(request.Email, ct) is not null)
        {
            throw new RequestValidationException("Email is already registered");
        }

        var created = await _users.AddAsync(new User
        {
            Email = request.Email.Trim(),
            DisplayName = request.DisplayName,
            NationalId = request.NationalId,
            PasswordHash = _passwords.Hash(password),
        }, ct);

        await _audit.RecordAsync(created.Email, "user.register", created.Id.ToString(), string.Empty, ct);
        return Mappings.ToProfile(created);
    }

    public async Task<User> UpdateAsync(User user, CancellationToken ct = default)
    {
        await _users.UpdateAsync(user, ct);
        await _audit.RecordAsync(user.Email, "user.update", user.Id.ToString(), "profile", ct);
        return user;
    }
}
