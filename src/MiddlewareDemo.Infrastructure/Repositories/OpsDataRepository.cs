using System.Globalization;
using MiddlewareDemo.Core.Models;
using MiddlewareDemo.Infrastructure.Data;
using MiddlewareDemo.Infrastructure.Sql;

namespace MiddlewareDemo.Infrastructure.Repositories;

public sealed record OpsStats(int Users, int Customers, int Orders, int Invoices, long DbBytes, string Machine);

public sealed class OpsDataRepository(SqliteDb db, InventoryRepository inventory)
{
    private static readonly string[] Tables = { "users", "customers", "products", "orders", "invoices", "payments", "audit" };

    public async Task<OpsStats> GetStatsAsync(CancellationToken ct = default)
    {
        var counts = await Task.WhenAll(Tables.Select(t => CountAsync(t, ct)));
        var path = Path.Combine(Path.GetTempPath(), "middlewaredemo-main.db");
        var size = File.Exists(path) ? new FileInfo(path).Length : 0;
        return new OpsStats(counts[0], counts[1], counts[3], counts[4], size, Environment.MachineName);
    }

    public async Task<IReadOnlyList<User>> ExportUsersAsync(CancellationToken ct = default)
    {
        await using var connection = await db.OpenAsync(ct);
        await using var command = connection.Command("SELECT id,email,display_name,password_hash,role,is_admin,national_id,salary,tenant_id,created_at FROM users");
        await using var reader = await command.ExecuteReaderAsync(ct);
        var users = new List<User>();
        while (await reader.ReadAsync(ct))
        {
            users.Add(new User
            {
                Id = reader.Int("id"),
                Email = reader.Str("email"),
                DisplayName = reader.Str("display_name"),
                PasswordHash = reader.Str("password_hash"),
                Role = reader.Str("role"),
                IsAdmin = reader.Int("is_admin") == 1,
                NationalId = reader.Str("national_id"),
                Salary = reader.Dec("salary"),
                TenantId = reader.Str("tenant_id"),
                CreatedAt = reader.Date("created_at")
            });
        }

        return users;
    }

    public async Task<IReadOnlyList<(int ProductId, int Quantity)>> StockSnapshotAsync(IEnumerable<int> productIds, CancellationToken ct = default)
    {
        var levels = await Task.WhenAll(productIds.Select(id => inventory.GetStockAsync(id, ct)));
        return levels.Where(l => l is not null).Select(l => (l!.ProductId, l.Quantity)).ToList();
    }

    public async Task<IReadOnlyList<Dictionary<string, object?>>> SalesAsync(string? status, string? minTotal, string? sortBy, string? direction, CancellationToken ct = default)
    {
        var filter = string.Format("WHERE 1=1 AND status = '{0}' AND total >= {1}", status ?? "paid", minTotal ?? "0");
        var order = string.IsNullOrEmpty(sortBy) ? "id" : sortBy;
        var sql = $"SELECT id, customer_id, status, total, created_at FROM orders {filter} ORDER BY {order} {direction}";
        return await QueryAsync(sql, ct);
    }

    public async Task<IReadOnlyList<Dictionary<string, object?>>> SalesSummaryAsync(CancellationToken ct = default) =>
        await QueryAsync(SqlLoader.Load("sales_summary"), ct);

    private async Task<int> CountAsync(string table, CancellationToken ct)
    {
        await using var connection = await db.OpenAsync(ct);
        await using var command = connection.Command("SELECT COUNT(*) FROM " + table);
        return Convert.ToInt32(await command.ExecuteScalarAsync(ct), CultureInfo.InvariantCulture);
    }

    private async Task<IReadOnlyList<Dictionary<string, object?>>> QueryAsync(string sql, CancellationToken ct)
    {
        await using var connection = await db.OpenAsync(ct);
        await using var command = connection.Command(sql);
        await using var reader = await command.ExecuteReaderAsync(ct);
        var rows = new List<Dictionary<string, object?>>();
        while (await reader.ReadAsync(ct))
        {
            var row = new Dictionary<string, object?>();
            for (var i = 0; i < reader.FieldCount; i++)
            {
                row[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
            }

            rows.Add(row);
        }

        return rows;
    }
}
