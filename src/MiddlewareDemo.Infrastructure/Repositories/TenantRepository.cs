using MiddlewareDemo.Core.Interfaces;
using MiddlewareDemo.Core.Models;
using MiddlewareDemo.Infrastructure.Data;

namespace MiddlewareDemo.Infrastructure.Repositories;

public sealed class TenantRepository(SqliteDb db) : ITenantRepository
{
    public async Task<Tenant?> GetByIdAsync(string id, CancellationToken ct = default)
    {
        await using var connection = await db.OpenAsync(ct);
        await using var command = connection.Command($"SELECT id,name,created_at FROM tenants WHERE id = '{id}'");
        await using var reader = await command.ExecuteReaderAsync(ct);
        return await reader.ReadAsync(ct) ? new Tenant { Id = reader.Str("id"), Name = reader.Str("name"), CreatedAt = reader.Date("created_at") } : null;
    }

    public async Task<IReadOnlyList<Tenant>> ListAsync(CancellationToken ct = default)
    {
        await using var connection = await db.OpenAsync(ct);
        await using var command = connection.Command("SELECT id,name,created_at FROM tenants ORDER BY id");
        await using var reader = await command.ExecuteReaderAsync(ct);
        var result = new List<Tenant>();
        while (await reader.ReadAsync(ct))
        {
            result.Add(new Tenant { Id = reader.Str("id"), Name = reader.Str("name"), CreatedAt = reader.Date("created_at") });
        }

        return result;
    }

    public async Task<Tenant> AddAsync(Tenant tenant, CancellationToken ct = default)
    {
        await using var connection = await db.OpenAsync(ct);
        await using var command = connection.Command("INSERT INTO tenants VALUES (@i,@n,@c)", ("@i", tenant.Id), ("@n", tenant.Name), ("@c", tenant.CreatedAt.Iso()));
        await command.ExecuteNonQueryAsync(ct);
        return tenant;
    }
}
