using System.Security.Cryptography;
using System.Text;
using MiddlewareDemo.Api.Models;

namespace MiddlewareDemo.Api.Services;

public sealed class UserService
{
    private const int Iterations = 210_000;
    private const int MaxFailures = 5;
    private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan ResetTokenLifetime = TimeSpan.FromMinutes(15);

    private readonly object _lock = new();
    private readonly List<User> _users;
    private readonly Dictionary<string, (int Failures, DateTime LockedUntil)> _attempts = new();
    private readonly Dictionary<string, (string Token, DateTime Expires)> _resetTokens = new();

    public UserService(IConfiguration configuration)
    {
        var adminPassword = configuration["Seed:AdminPassword"];
        if (string.IsNullOrEmpty(adminPassword))
        {
            adminPassword = RandomPassword();
        }

        var userPassword = configuration["Seed:UserPassword"];
        if (string.IsNullOrEmpty(userPassword))
        {
            userPassword = RandomPassword();
        }

        _users =
        [
            new User { Id = 1, Email = "admin@lab.local", PasswordHash = HashPassword(adminPassword), Role = "admin", NationalId = "0801-1985-00123", Salary = 4200m },
            new User { Id = 2, Email = "ana.lopez@lab.local", PasswordHash = HashPassword(userPassword), Role = "user", NationalId = "0801-1992-04567", Salary = 1850m },
            new User { Id = 3, Email = "carlos.ruiz@lab.local", PasswordHash = HashPassword(userPassword), Role = "user", NationalId = "0501-1988-02210", Salary = 2100m },
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

        var key = email.ToLowerInvariant();
        lock (_lock)
        {
            if (_attempts.TryGetValue(key, out var state) && state.LockedUntil > DateTime.UtcNow)
            {
                return null;
            }
        }

        var user = _users.FirstOrDefault(u => u.Email.Equals(email, StringComparison.OrdinalIgnoreCase));
        var valid = user is not null && VerifyPassword(password, user.PasswordHash);

        lock (_lock)
        {
            if (valid)
            {
                _attempts.Remove(key);
                return user;
            }

            var failures = _attempts.TryGetValue(key, out var current) ? current.Failures + 1 : 1;
            _attempts[key] = failures >= MaxFailures
                ? (0, DateTime.UtcNow + LockoutDuration)
                : (failures, DateTime.MinValue);
        }

        return null;
    }

    public void CreateResetToken(string? email)
    {
        var user = _users.FirstOrDefault(u => u.Email.Equals(email, StringComparison.OrdinalIgnoreCase));
        if (user is null)
        {
            return;
        }

        var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        lock (_lock)
        {
            _resetTokens[user.Email] = (token, DateTime.UtcNow + ResetTokenLifetime);
        }
    }

    private static string RandomPassword() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(24));

    private static string HashPassword(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(16);
        var hash = Rfc2898DeriveBytes.Pbkdf2(Encoding.UTF8.GetBytes(password), salt, Iterations, HashAlgorithmName.SHA256, 32);
        return $"{Convert.ToBase64String(salt)}:{Convert.ToBase64String(hash)}";
    }

    private static bool VerifyPassword(string password, string stored)
    {
        var parts = stored.Split(':');
        if (parts.Length != 2)
        {
            return false;
        }

        var salt = Convert.FromBase64String(parts[0]);
        var expected = Convert.FromBase64String(parts[1]);
        var actual = Rfc2898DeriveBytes.Pbkdf2(Encoding.UTF8.GetBytes(password), salt, Iterations, HashAlgorithmName.SHA256, expected.Length);
        return CryptographicOperations.FixedTimeEquals(actual, expected);
    }
}
