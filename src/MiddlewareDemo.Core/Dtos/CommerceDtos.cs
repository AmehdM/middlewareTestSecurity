namespace MiddlewareDemo.Core.Dtos;

public sealed record CustomerDto(int Id, string Name, string Email, string Phone, string NationalId, string TenantId);

public sealed record CreateCustomerRequest(string Name, string Email, string Phone, string NationalId, string TenantId);

public sealed record UpdateCustomerRequest(string Name, string Email, string Phone);

public sealed record ProductDto(int Id, string Sku, string Name, decimal Price, string Category, string Description);

public sealed record CreateProductRequest(string Sku, string Name, decimal Price, string Category, string Description);

public sealed record UpdateProductRequest(string Name, decimal Price, string Category, string Description);

public sealed record ImportResult(int Imported, int Skipped);

public sealed record OrderLineRequest(int ProductId, int Quantity);

public sealed record CreateOrderRequest(int CustomerId, List<OrderLineRequest> Lines, string? CouponCode);

public sealed record OrderLineDto(int ProductId, int Quantity, decimal UnitPrice);

public sealed record OrderDto(int Id, int CustomerId, string Status, decimal Total, DateTime CreatedAt, List<OrderLineDto> Lines);
