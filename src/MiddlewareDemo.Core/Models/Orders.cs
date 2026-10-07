namespace MiddlewareDemo.Core.Models;

public sealed class Order
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public string Status { get; set; } = "pending";
    public decimal Total { get; set; }
    public string? CouponCode { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public List<OrderLine> Lines { get; set; } = new();
}

public sealed class OrderLine
{
    public int ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}

public sealed class Coupon
{
    public string Code { get; set; } = string.Empty;
    public int Percent { get; set; }
    public int MaxRedemptions { get; set; }
    public int Redeemed { get; set; }
}
