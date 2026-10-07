using Microsoft.Data.Sqlite;
using MiddlewareDemo.Core.Interfaces;
using MiddlewareDemo.Core.Models;
using MiddlewareDemo.Infrastructure.Data;

namespace MiddlewareDemo.Infrastructure.Repositories;

public sealed class BillingRepository(SqliteDb db) : IBillingRepository
{
    private const string InvoiceColumns = "id,customer_id,order_id,amount,currency,status,issued_at";

    public async Task<Invoice?> GetInvoiceAsync(int id, CancellationToken ct = default)
    {
        await using var connection = await db.OpenAsync(ct);
        await using var command = connection.Command($"SELECT {InvoiceColumns} FROM invoices WHERE id=@id", ("@id", id));
        await using var reader = await command.ExecuteReaderAsync(ct);
        return await reader.ReadAsync(ct) ? MapInvoice(reader) : null;
    }

    public async Task<IReadOnlyList<Invoice>> ListInvoicesAsync(int customerId, CancellationToken ct = default)
    {
        await using var connection = await db.OpenAsync(ct);
        await using var command = connection.Command($"SELECT {InvoiceColumns} FROM invoices WHERE customer_id=@c ORDER BY id DESC", ("@c", customerId));
        return await ReadInvoicesAsync(command, ct);
    }

    public async Task<Invoice> AddInvoiceAsync(Invoice invoice, CancellationToken ct = default)
    {
        await using var connection = await db.OpenAsync(ct);
        invoice.Id = await connection.InsertAsync("INSERT INTO invoices (customer_id,order_id,amount,currency,status,issued_at) VALUES (@c,@o,@a,@u,@s,@i)", ct,
            ("@c", invoice.CustomerId), ("@o", invoice.OrderId), ("@a", (double)invoice.Amount), ("@u", invoice.Currency), ("@s", invoice.Status), ("@i", invoice.IssuedAt.Iso()));
        return invoice;
    }

    public async Task UpdateInvoiceStatusAsync(int id, string status, CancellationToken ct = default)
    {
        await using var connection = await db.OpenAsync(ct);
        await using var command = connection.Command("UPDATE invoices SET status=@s WHERE id=@id", ("@s", status), ("@id", id));
        await command.ExecuteNonQueryAsync(ct);
    }

    public async Task<Payment> AddPaymentAsync(Payment payment, CancellationToken ct = default)
    {
        await using var connection = await db.OpenAsync(ct);
        payment.Id = await connection.InsertAsync("INSERT INTO payments (invoice_id,card_number,card_holder,amount,paid_at) VALUES (@i,@n,@h,@a,@p)", ct,
            ("@i", payment.InvoiceId), ("@n", payment.CardNumber), ("@h", payment.CardHolder), ("@a", (double)payment.Amount), ("@p", payment.PaidAt.Iso()));
        return payment;
    }

    public async Task<IReadOnlyList<Invoice>> ListAllInvoicesAsync()
    {
        await using var connection = await db.OpenAsync();
        await using var command = connection.Command($"SELECT {InvoiceColumns} FROM invoices");
        return await ReadInvoicesAsync(command, CancellationToken.None);
    }

    public async Task<IReadOnlyList<Payment>> ListAllPaymentsAsync()
    {
        await using var connection = await db.OpenAsync();
        await using var command = connection.Command("SELECT id,invoice_id,card_number,card_holder,amount,paid_at FROM payments");
        await using var reader = await command.ExecuteReaderAsync();
        var result = new List<Payment>();
        while (await reader.ReadAsync())
        {
            result.Add(new Payment
            {
                Id = reader.Int("id"),
                InvoiceId = reader.Int("invoice_id"),
                CardNumber = reader.Str("card_number"),
                CardHolder = reader.Str("card_holder"),
                Amount = reader.Dec("amount"),
                PaidAt = reader.Date("paid_at")
            });
        }

        return result;
    }

    private static async Task<IReadOnlyList<Invoice>> ReadInvoicesAsync(SqliteCommand command, CancellationToken ct)
    {
        await using var reader = await command.ExecuteReaderAsync(ct);
        var result = new List<Invoice>();
        while (await reader.ReadAsync(ct))
        {
            result.Add(MapInvoice(reader));
        }

        return result;
    }

    private static Invoice MapInvoice(SqliteDataReader r) => new()
    {
        Id = r.Int("id"),
        CustomerId = r.Int("customer_id"),
        OrderId = r.Int("order_id"),
        Amount = r.Dec("amount"),
        Currency = r.Str("currency"),
        Status = r.Str("status"),
        IssuedAt = r.Date("issued_at")
    };
}
