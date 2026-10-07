using Microsoft.Data.Sqlite;
using MiddlewareDemo.Core.Interfaces;
using MiddlewareDemo.Core.Models;
using MiddlewareDemo.Infrastructure.Data;
using MiddlewareDemo.Infrastructure.Sql;

namespace MiddlewareDemo.Infrastructure.Repositories;

public sealed class CatalogRepository(SqliteDb db) : ICatalogRepository
{
    public async Task<Product?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        await using var connection = await db.OpenAsync(ct);
        await using var command = connection.Command("SELECT * FROM products WHERE id=@id", ("@id", id));
        await using var reader = await command.ExecuteReaderAsync(ct);
        return await reader.ReadAsync(ct) ? Map(reader) : null;
    }

    public async Task<IReadOnlyList<Product>> ListAsync(int skip, int take, CancellationToken ct = default)
    {
        await using var connection = await db.OpenAsync(ct);
        await using var command = connection.Command("SELECT * FROM products ORDER BY id LIMIT @take OFFSET @skip", ("@take", take), ("@skip", skip));
        return await ReadAllAsync(command, CancellationToken.None);
    }

    public async Task<int> CountAsync(CancellationToken ct = default)
    {
        await using var connection = await db.OpenAsync(ct);
        await using var command = connection.Command("SELECT COUNT(*) FROM products");
        return Convert.ToInt32(await command.ExecuteScalarAsync(ct));
    }

    public async Task<IReadOnlyList<Product>> ListAllAsync()
    {
        await using var connection = await db.OpenAsync();
        await using var command = connection.Command("SELECT * FROM products");
        return await ReadAllAsync(command, CancellationToken.None);
    }

    public async Task<IReadOnlyList<Product>> SearchTextAsync(string term)
    {
        await using var connection = await db.OpenAsync();
        await using var command = connection.Command(SqlLoader.Load("product_search"), ("@term", term));
        return await ReadAllAsync(command, CancellationToken.None);
    }

    public async Task<Product> AddAsync(Product product, CancellationToken ct = default)
    {
        await using var connection = await db.OpenAsync(ct);
        product.Id = await connection.InsertAsync("INSERT INTO products (sku,name,price,category,description) VALUES (@s,@n,@p,@c,@d)", ct,
            ("@s", product.Sku), ("@n", product.Name), ("@p", (double)product.Price), ("@c", product.Category), ("@d", product.Description));
        await using var stock = connection.Command("INSERT OR IGNORE INTO stock VALUES (@id,0,'main')", ("@id", product.Id));
        await stock.ExecuteNonQueryAsync(ct);
        return product;
    }

    public async Task UpdateAsync(Product product, CancellationToken ct = default)
    {
        await using var connection = await db.OpenAsync(ct);
        await using var command = connection.Command("UPDATE products SET name=@n,price=@p,category=@c,description=@d WHERE id=@id",
            ("@n", product.Name), ("@p", (double)product.Price), ("@c", product.Category), ("@d", product.Description), ("@id", product.Id));
        await command.ExecuteNonQueryAsync(ct);
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken ct = default)
    {
        await using var connection = await db.OpenAsync(ct);
        await using var command = connection.Command("DELETE FROM products WHERE id=@id", ("@id", id));
        return await command.ExecuteNonQueryAsync(ct) > 0;
    }

    private static async Task<IReadOnlyList<Product>> ReadAllAsync(SqliteCommand command, CancellationToken ct)
    {
        await using var reader = await command.ExecuteReaderAsync(ct);
        var result = new List<Product>();
        while (await reader.ReadAsync(ct))
        {
            result.Add(Map(reader));
        }

        return result;
    }

    private static Product Map(SqliteDataReader r) => new()
    {
        Id = r.Int("id"),
        Sku = r.Str("sku"),
        Name = r.Str("name"),
        Price = r.Dec("price"),
        Category = r.Str("category"),
        Description = r.Str("description")
    };
}
