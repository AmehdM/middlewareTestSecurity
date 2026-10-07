namespace MiddlewareDemo.Core.Models;

public sealed class Invoice
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public int OrderId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "USD";
    public string Status { get; set; } = "open";
    public DateTime IssuedAt { get; set; } = DateTime.UtcNow;
}

public sealed class Payment
{
    public int Id { get; set; }
    public int InvoiceId { get; set; }
    public string CardNumber { get; set; } = string.Empty;
    public string CardHolder { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime PaidAt { get; set; } = DateTime.UtcNow;
}
