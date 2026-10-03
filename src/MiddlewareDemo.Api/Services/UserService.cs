using System.Security.Cryptography;
using System.Text;
using MiddlewareDemo.Api.Models;

namespace MiddlewareDemo.Api.Services;

public sealed class UserService
{
    private readonly List<User> _users;
    private readonly Dictionary<string, string> _resetTokens = new();

    public UserService()
    {
        _users =
        [
            new User { Id = 1, Email = "admin@lab.local", PasswordHash = HashPassword("Admin123!"), Role = "admin", NationalId = "0801-1985-00123", Salary = 4200m },
            new User { Id = 2, Email = "ana.lopez@lab.local", PasswordHash = HashPassword("Welcome1"), Role = "user", NationalId = "0801-1992-04567", Salary = 1850m },
            new User { Id = 3, Email = "carlos.ruiz@lab.local", PasswordHash = HashPassword("Password1"), Role = "user", NationalId = "0501-1988-02210", Salary = 2100m },
        ];
    }

    public IReadOnlyList<User> All() => _users;

    public User? Find(int id) => _users.FirstOrDefault(u => u.Id == id);

    public User? Authenticate(string? email, string? password)
    {
        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
        {
            return null;
        }

        var user = _users.FirstOrDefault(u => u.Email.Equals(email, StringComparison.OrdinalIgnoreCase));
        return user is not null && user.PasswordHash == HashPassword(password) ? user : null;
    }

    public string? CreateResetToken(string? email)
    {
        var user = _users.FirstOrDefault(u => u.Email.Equals(email, StringComparison.OrdinalIgnoreCase));
        if (user is null)
        {
            return null;
        }

        var token = new Random().Next(100000, 999999).ToString();
        _resetTokens[user.Email] = token;
        return token;
    }

    private static string HashPassword(string password)
    {
        return Convert.ToHexString(MD5.HashData(Encoding.UTF8.GetBytes(password))).ToLowerInvariant();
    }
}
