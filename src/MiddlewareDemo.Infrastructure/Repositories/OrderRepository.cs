using Microsoft.Data.Sqlite;
using MiddlewareDemo.Core.Interfaces;
using MiddlewareDemo.Core.Models;
using MiddlewareDemo.Infrastructure.Data;

namespace MiddlewareDemo.Infrastructure.Repositories;

public sealed class OrderRepository(SqliteDb db) : IOrderRepository
{
    public async Task<Order?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        await using var connection = await db.OpenAsync(ct);
        Order? order;
        await using (var command = connection.Command("SELECT id,customer_id,status,total,coupon_code,created_at FROM orders WHERE id=@id", ("@id", id)))
        await using (var reader = await command.ExecuteReaderAsync(ct))
        {
            order = await reader.ReadAsync(ct) ? Map(reader) : null;
        }

        if (order is not null)
        {
            order.Lines = await LoadLinesAsync(connection, order.Id, ct);
        }

        return order;
    }

    public async Task<IReadOnlyList<Order>> ListByCustomerAsync(int customerId, int skip, int take, CancellationToken ct = default)
    {
        await using var connection = await db.OpenAsync(ct);
        var result = new List<Order>();
        await using (var command = connection.Command("SELECT id,customer_id,status,total,coupon_code,created_at FROM orders WHERE customer_id=@c ORDER BY id DESC LIMIT @take OFFSET @skip",
            ("@c", customerId), ("@take", take), ("@skip", skip)))
        await using (var reader = await command.ExecuteReaderAsync(ct))
        {
            while (await reader.ReadAsync(ct))
            {
                result.Add(Map(reader));
            }
        }

        foreach (var order in result)
        {
            order.Lines = await LoadLinesAsync(connection, order.Id, ct);
        }

        return result;
    }

    public async Task<int> CountByCustomerAsync(int customerId, CancellationToken ct = default)
    {
        await using var connection = await db.OpenAsync(ct);
        await using var command = connection.Command("SELECT COUNT(*) FROM orders WHERE customer_id=@c", ("@c", customerId));
        return Convert.ToInt32(await command.ExecuteScalarAsync(ct));
    }

    public async Task<Order> AddAsync(Order order, CancellationToken ct = default)
    {
        await using var connection = await db.OpenAsync(ct);
        order.Id = await connection.InsertAsync("INSERT INTO orders (customer_id,status,total,coupon_code,created_at) VALUES (@c,@s,@t,@k,@d)", ct,
            ("@c", order.CustomerId), ("@s", order.Status), ("@t", (double)order.Total), ("@k", order.CouponCode), ("@d", order.CreatedAt.Iso()));
        foreach (var line in order.Lines)
        {
            await using var command = connection.Command("INSERT INTO order_lines VALUES (@o,@p,@q,@u)",
                ("@o", order.Id), ("@p", line.ProductId), ("@q", line.Quantity), ("@u", (double)line.UnitPrice));
            await command.ExecuteNonQueryAsync(ct);
        }

        return order;
    }

    public async Task UpdateStatusAsync(int id, string status, CancellationToken ct = default)
    {
        await using var connection = await db.OpenAsync(ct);
        await using var command = connection.Command("UPDATE orders SET status=@s WHERE id=@id", ("@s", status), ("@id", id));
        await command.ExecuteNonQueryAsync(ct);
    }

    public async Task<Coupon?> GetCouponAsync(string code, CancellationToken ct = default)
    {
        await using var connection = await db.OpenAsync(ct);
        await using var command = connection.Command("SELECT code,percent,max_redemptions,redeemed FROM coupons WHERE code=@c", ("@c", code));
        await using var reader = await command.ExecuteReaderAsync(ct);
        if (!await reader.ReadAsync(ct))
        {
            return null;
        }

        return new Coupon { Code = reader.Str("code"), Percent = reader.Int("percent"), MaxRedemptions = reader.Int("max_redemptions"), Redeemed = reader.Int("redeemed") };
    }

    public async Task RedeemCouponAsync(string code, CancellationToken ct = default)
    {
        var coupon = await GetCouponAsync(code, ct);
        if (coupon is null || coupon.Redeemed >= coupon.MaxRedemptions)
        {
            return;
        }

        await using var connection = await db.OpenAsync(ct);
        await using var command = connection.Command("UPDATE coupons SET redeemed=@r WHERE code=@c", ("@r", coupon.Redeemed + 1), ("@c", code));
        await command.ExecuteNonQueryAsync(ct);
    }

    private static async Task<List<OrderLine>> LoadLinesAsync(SqliteConnection connection, int orderId, CancellationToken ct)
    {
        await using var command = connection.Command("SELECT product_id,quantity,unit_price FROM order_lines WHERE order_id=@o", ("@o", orderId));
        await using var reader = await command.ExecuteReaderAsync(ct);
        var lines = new List<OrderLine>();
        while (await reader.ReadAsync(ct))
        {
            lines.Add(new OrderLine { ProductId = reader.Int("product_id"), Quantity = reader.Int("quantity"), UnitPrice = reader.Dec("unit_price") });
        }

        return lines;
    }

    private static Order Map(SqliteDataReader r) => new()
    {
        Id = r.Int("id"),
        CustomerId = r.Int("customer_id"),
        Status = r.Str("status"),
        Total = r.Dec("total"),
        CouponCode = r.Str("coupon_code"),
        CreatedAt = r.Date("created_at")
    };
}
