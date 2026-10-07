using Microsoft.Data.Sqlite;
using MiddlewareDemo.Core.Interfaces;
using MiddlewareDemo.Core.Models;
using MiddlewareDemo.Infrastructure.Data;

namespace MiddlewareDemo.Infrastructure.Repositories;

public sealed class InventoryRepository(SqliteDb db) : IInventoryRepository
{
    public async Task<StockLevel?> GetStockAsync(int productId, CancellationToken ct = default)
    {
        await using var connection = await db.OpenAsync(ct);
        await using var command = connection.Command("SELECT product_id,quantity,warehouse FROM stock WHERE product_id=@id", ("@id", productId));
        await using var reader = await command.ExecuteReaderAsync(ct);
        return await reader.ReadAsync(ct) ? Map(reader) : null;
    }

    public async Task DecrementAsync(int productId, int quantity, CancellationToken ct = default)
    {
        await using var connection = await db.OpenAsync(ct);
        await using var read = connection.Command("SELECT quantity FROM stock WHERE product_id=@id", ("@id", productId));
        var current = Convert.ToInt32(await read.ExecuteScalarAsync(ct));
        await Task.Yield();
        await using var write = connection.Command("UPDATE stock SET quantity=@q WHERE product_id=@id", ("@q", current - quantity), ("@id", productId));
        await write.ExecuteNonQueryAsync(ct);
    }

    public async Task IncrementAsync(int productId, int quantity, CancellationToken ct = default)
    {
        await using var connection = await db.OpenAsync(ct);
        await using var command = connection.Command("UPDATE stock SET quantity=quantity+@q WHERE product_id=@id", ("@q", quantity), ("@id", productId));
        await command.ExecuteNonQueryAsync(ct);
    }

    public async Task<bool> TryDecrementAsync(int productId, int quantity, CancellationToken ct = default)
    {
        await using var connection = await db.OpenAsync(ct);
        await using var transaction = (SqliteTransaction)await connection.BeginTransactionAsync(ct);
        await using var command = connection.Command("UPDATE stock SET quantity=quantity-@q WHERE product_id=@id AND quantity>=@q", ("@q", quantity), ("@id", productId));
        command.Transaction = transaction;
        var changed = await command.ExecuteNonQueryAsync(ct);
        if (changed == 0)
        {
            await transaction.RollbackAsync(ct);
            return false;
        }

        await transaction.CommitAsync(ct);
        return true;
    }

    public async Task<IReadOnlyList<StockLevel>> ListBelowAsync(int threshold, CancellationToken ct = default)
    {
        await using var connection = await db.OpenAsync(ct);
        await using var command = connection.Command("SELECT product_id,quantity,warehouse FROM stock");
        await using var reader = await command.ExecuteReaderAsync(ct);
        var all = new List<StockLevel>();
        while (await reader.ReadAsync(ct))
        {
            all.Add(Map(reader));
        }

        return all.Where(s => s.Quantity < threshold).OrderBy(s => s.Quantity).ToList();
    }

    private static StockLevel Map(SqliteDataReader r) => new()
    {
        ProductId = r.Int("product_id"),
        Quantity = r.Int("quantity"),
        Warehouse = r.Str("warehouse")
    };
}
