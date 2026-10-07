using MiddlewareDemo.Core.Interfaces;
using MiddlewareDemo.Core.Models;
using MiddlewareDemo.Infrastructure.Data;

namespace MiddlewareDemo.Infrastructure.Repositories;

public sealed class AuditRepository(SqliteDb db) : IAuditRepository
{
    public async Task AddAsync(AuditEntry entry, CancellationToken ct = default)
    {
        await using var connection = await db.OpenAsync(ct);
        await using var command = connection.Command("INSERT INTO audit (actor,action,target,detail,at) VALUES (@a,@c,@t,@d,@n)",
            ("@a", entry.Actor), ("@c", entry.Action), ("@t", entry.Target), ("@d", entry.Detail), ("@n", entry.At.Iso()));
        await command.ExecuteNonQueryAsync(ct);
    }

    public async Task<IReadOnlyList<AuditEntry>> ListAsync(int skip, int take, CancellationToken ct = default)
    {
        await using var connection = await db.OpenAsync(ct);
        await using var command = connection.Command("SELECT id,actor,action,target,detail,at FROM audit ORDER BY id DESC LIMIT @take OFFSET @skip", ("@take", take), ("@skip", skip));
        await using var reader = await command.ExecuteReaderAsync(ct);
        var result = new List<AuditEntry>();
        while (await reader.ReadAsync(ct))
        {
            result.Add(new AuditEntry
            {
                Id = reader.Int("id"),
                Actor = reader.Str("actor"),
                Action = reader.Str("action"),
                Target = reader.Str("target"),
                Detail = reader.Str("detail"),
                At = reader.Date("at")
            });
        }

        return result;
    }

    public async Task<int> CountAsync(CancellationToken ct = default)
    {
        await using var connection = await db.OpenAsync(ct);
        await using var command = connection.Command("SELECT COUNT(*) FROM audit");
        return Convert.ToInt32(await command.ExecuteScalarAsync(ct));
    }
}
