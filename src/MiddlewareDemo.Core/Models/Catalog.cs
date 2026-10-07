namespace MiddlewareDemo.Core.Models;

public sealed class Product
{
    public int Id { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Category { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public sealed class StockLevel
{
    public int ProductId { get; set; }
    public int Quantity { get; set; }
    public string Warehouse { get; set; } = "main";
}
