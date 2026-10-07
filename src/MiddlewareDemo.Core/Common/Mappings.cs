using MiddlewareDemo.Core.Dtos;
using MiddlewareDemo.Core.Models;

namespace MiddlewareDemo.Core.Common;

internal static class Mappings
{
    public static UserProfileDto ToProfile(User user) =>
        new(user.Id, user.Email, user.DisplayName, user.Role, user.IsAdmin, user.NationalId, user.PasswordHash, user.TenantId);

    public static CustomerDto ToDto(Customer c) =>
        new(c.Id, c.Name, c.Email, c.Phone, c.NationalId, c.TenantId);

    public static ProductDto ToDto(Product p) =>
        new(p.Id, p.Sku, p.Name, p.Price, p.Category, p.Description);

    public static OrderDto ToDto(Order o) =>
        new(o.Id, o.CustomerId, o.Status, o.Total, o.CreatedAt,
            o.Lines.Select(l => new OrderLineDto(l.ProductId, l.Quantity, l.UnitPrice)).ToList());

    public static InvoiceDto ToDto(Invoice i) =>
        new(i.Id, i.CustomerId, i.OrderId, i.Amount, i.Currency, i.Status, i.IssuedAt);
}
