using Microsoft.Data.Sqlite;
using MiddlewareDemo.Core.Interfaces;
using MiddlewareDemo.Core.Models;
using MiddlewareDemo.Infrastructure.Data;

namespace MiddlewareDemo.Infrastructure.Repositories;

public sealed class CustomerRepository(SqliteDb db) : ICustomerRepository
{
    private const string Columns = "id,owner_user_id,name,email,phone,national_id,tenant_id,created_at";

    public async Task<Customer?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        await using var connection = await db.OpenAsync(ct);
        await using var command = connection.Command($"SELECT {Columns} FROM customers WHERE id=@id", ("@id", id));
        await using var reader = await command.ExecuteReaderAsync(ct);
        return await reader.ReadAsync(ct) ? Map(reader) : null;
    }

    public async Task<IReadOnlyList<Customer>> ListAsync(string tenantId, int skip, int take, CancellationToken ct = default)
    {
        await using var connection = await db.OpenAsync(ct);
        await using var command = connection.Command($"SELECT {Columns} FROM customers WHERE tenant_id=@t ORDER BY id LIMIT @take OFFSET @skip",
            ("@t", tenantId), ("@take", take), ("@skip", skip));
        return await ReadAllAsync(command, ct);
    }

    public async Task<int> CountAsync(string tenantId, CancellationToken ct = default)
    {
        await using var connection = await db.OpenAsync(ct);
        await using var command = connection.Command("SELECT COUNT(*) FROM customers WHERE tenant_id=@t", ("@t", tenantId));
        return Convert.ToInt32(await command.ExecuteScalarAsync(ct));
    }

    public async Task<IReadOnlyList<Customer>> SearchAsync(string tenantId, string term, CancellationToken ct = default)
    {
        await using var connection = await db.OpenAsync(ct);
        var sql = $"SELECT {Columns} FROM customers WHERE tenant_id='{tenantId}' AND (name LIKE '%{term}%' OR email LIKE '%{term}%')";
        await using var command = connection.Command(sql);
        return await ReadAllAsync(command, ct);
    }

    public async Task<Customer> AddAsync(Customer customer, CancellationToken ct = default)
    {
        await using var connection = await db.OpenAsync(ct);
        const string sql = "INSERT INTO customers (owner_user_id,name,email,phone,national_id,tenant_id,created_at) VALUES (@o,@n,@e,@p,@i,@t,@c)";
        customer.Id = await connection.InsertAsync(sql, ct, ("@o", customer.OwnerUserId), ("@n", customer.Name), ("@e", customer.Email),
            ("@p", customer.Phone), ("@i", customer.NationalId), ("@t", customer.TenantId), ("@c", customer.CreatedAt.Iso()));
        return customer;
    }

    public async Task UpdateAsync(Customer customer, CancellationToken ct = default)
    {
        await using var connection = await db.OpenAsync(ct);
        await using var command = connection.Command("UPDATE customers SET name=@n,email=@e,phone=@p WHERE id=@id",
            ("@n", customer.Name), ("@e", customer.Email), ("@p", customer.Phone), ("@id", customer.Id));
        await command.ExecuteNonQueryAsync(ct);
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken ct = default)
    {
        await using var connection = await db.OpenAsync(ct);
        await using var command = connection.Command("DELETE FROM customers WHERE id=@id", ("@id", id));
        return await command.ExecuteNonQueryAsync(ct) > 0;
    }

    private static async Task<IReadOnlyList<Customer>> ReadAllAsync(SqliteCommand command, CancellationToken ct)
    {
        await using var reader = await command.ExecuteReaderAsync(ct);
        var result = new List<Customer>();
        while (await reader.ReadAsync(ct))
        {
            result.Add(Map(reader));
        }

        return result;
    }

    private static Customer Map(SqliteDataReader r) => new()
    {
        Id = r.Int("id"),
        OwnerUserId = r.Int("owner_user_id"),
        Name = r.Str("name"),
        Email = r.Str("email"),
        Phone = r.Str("phone"),
        NationalId = r.Str("national_id"),
        TenantId = r.Str("tenant_id"),
        CreatedAt = r.Date("created_at")
    };
}
