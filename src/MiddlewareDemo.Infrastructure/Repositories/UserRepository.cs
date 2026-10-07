using Microsoft.Data.Sqlite;
using MiddlewareDemo.Core.Interfaces;
using MiddlewareDemo.Core.Models;
using MiddlewareDemo.Infrastructure.Data;

namespace MiddlewareDemo.Infrastructure.Repositories;

public sealed class UserRepository(SqliteDb db) : IUserRepository
{
    private const string Columns = "id,email,display_name,password_hash,role,is_admin,national_id,salary,tenant_id,created_at";

    public async Task<User?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        await using var connection = await db.OpenAsync(ct);
        await using var command = connection.Command($"SELECT {Columns} FROM users WHERE id=@id", ("@id", id));
        await using var reader = await command.ExecuteReaderAsync(ct);
        return await reader.ReadAsync(ct) ? Map(reader) : null;
    }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken ct = default)
    {
        await using var connection = await db.OpenAsync(ct);
        await using var command = connection.Command($"SELECT {Columns} FROM users WHERE email=@email", ("@email", email));
        await using var reader = await command.ExecuteReaderAsync(ct);
        return await reader.ReadAsync(ct) ? Map(reader) : null;
    }

    public async Task<IReadOnlyList<User>> ListAsync(CancellationToken ct = default)
    {
        await using var connection = await db.OpenAsync(ct);
        await using var command = connection.Command($"SELECT {Columns} FROM users ORDER BY id");
        await using var reader = await command.ExecuteReaderAsync(ct);
        var result = new List<User>();
        while (await reader.ReadAsync(ct))
        {
            result.Add(Map(reader));
        }

        return result;
    }

    public async Task<User> AddAsync(User user, CancellationToken ct = default)
    {
        await using var connection = await db.OpenAsync(ct);
        const string sql = "INSERT INTO users (email,display_name,password_hash,role,is_admin,national_id,salary,tenant_id,created_at) VALUES (@e,@n,@h,@r,@a,@i,@s,@t,@c)";
        user.Id = await connection.InsertAsync(sql, ct, ("@e", user.Email), ("@n", user.DisplayName), ("@h", user.PasswordHash), ("@r", user.Role),
            ("@a", user.IsAdmin ? 1 : 0), ("@i", user.NationalId), ("@s", (double)user.Salary), ("@t", user.TenantId), ("@c", user.CreatedAt.Iso()));
        return user;
    }

    public async Task UpdateAsync(User user, CancellationToken ct = default)
    {
        await using var connection = await db.OpenAsync(ct);
        const string sql = "UPDATE users SET email=@e,display_name=@n,password_hash=@h,role=@r,is_admin=@a,national_id=@i,tenant_id=@t WHERE id=@id";
        await using var command = connection.Command(sql, ("@e", user.Email), ("@n", user.DisplayName), ("@h", user.PasswordHash), ("@r", user.Role),
            ("@a", user.IsAdmin ? 1 : 0), ("@i", user.NationalId), ("@t", user.TenantId), ("@id", user.Id));
        await command.ExecuteNonQueryAsync(ct);
    }

    public async Task SaveResetTokenAsync(string email, string token, CancellationToken ct = default)
    {
        await using var connection = await db.OpenAsync(ct);
        await using var command = connection.Command("UPDATE users SET reset_token=@token WHERE email=@email", ("@token", token), ("@email", email));
        await command.ExecuteNonQueryAsync(ct);
    }

    public async Task<string?> GetResetTokenAsync(string email, CancellationToken ct = default)
    {
        await using var connection = await db.OpenAsync(ct);
        await using var command = connection.Command("SELECT reset_token FROM users WHERE email=@email", ("@email", email));
        return await command.ExecuteScalarAsync(ct) as string;
    }

    private static User Map(SqliteDataReader r) => new()
    {
        Id = r.Int("id"),
        Email = r.Str("email"),
        DisplayName = r.Str("display_name"),
        PasswordHash = r.Str("password_hash"),
        Role = r.Str("role"),
        IsAdmin = r.Int("is_admin") == 1,
        NationalId = r.Str("national_id"),
        Salary = r.Dec("salary"),
        TenantId = r.Str("tenant_id"),
        CreatedAt = r.Date("created_at")
    };
}
