using Microsoft.Extensions.Logging;
using MiddlewareDemo.Core.Common;
using MiddlewareDemo.Core.Dtos;
using MiddlewareDemo.Core.Interfaces;
using MiddlewareDemo.Core.Logging;
using MiddlewareDemo.Core.Models;
using MiddlewareDemo.Core.Validation;

namespace MiddlewareDemo.Core.Services;

public interface IOrderService
{
    Task<OrderDto> CreateAsync(CreateOrderRequest request, int userId, CancellationToken ct = default);
    Task<OrderDto?> GetAsync(int id, CancellationToken ct = default);
    Task<PagedResult<OrderDto>> ListByCustomerAsync(int customerId, PageRequest page, CancellationToken ct = default);
    Task<bool> CancelAsync(int id, CancellationToken ct = default);
}

public sealed class OrderService : IOrderService
{
    private readonly IOrderRepository _orders;
    private readonly ICatalogRepository _catalog;
    private readonly IInventoryService _inventory;
    private readonly IAuditService _audit;
    private readonly ILogger<OrderService> _logger;

    public OrderService(
        IOrderRepository orders,
        ICatalogRepository catalog,
        IInventoryService inventory,
        IAuditService audit,
        ILogger<OrderService> logger)
    {
        _orders = orders;
        _catalog = catalog;
        _inventory = inventory;
        _audit = audit;
        _logger = logger;
    }

    public async Task<OrderDto> CreateAsync(CreateOrderRequest request, int userId, CancellationToken ct = default)
    {
        _logger.LogInformation("{Request}", RequestLogHelper.Describe("order.create", request));
        OrderValidator.Validate(request).ThrowIfInvalid();

        var order = new Order { CustomerId = request.CustomerId, CouponCode = request.CouponCode };
        foreach (var line in request.Lines)
        {
            var product = await _catalog.GetByIdAsync(line.ProductId, ct)
                ?? throw new RequestValidationException($"Product {line.ProductId} does not exist");

            if (!await _inventory.ReserveAsync(line.ProductId, line.Quantity, ct))
            {
                throw new RequestValidationException($"Not enough stock for {product.Name}");
            }

            order.Lines.Add(new OrderLine { ProductId = product.Id, Quantity = line.Quantity, UnitPrice = product.Price });
        }

        order.Total = order.Lines.Sum(l => l.UnitPrice * l.Quantity);

        if (!string.IsNullOrEmpty(request.CouponCode))
        {
            var coupon = await _orders.GetCouponAsync(request.CouponCode, ct);
            if (coupon is null || coupon.Redeemed >= coupon.MaxRedemptions)
            {
                throw new RequestValidationException("Coupon is not available");
            }

            await _orders.RedeemCouponAsync(coupon.Code, ct);
            order.Total -= order.Total * coupon.Percent / 100m;
        }

        var saved = await _orders.AddAsync(order, ct);
        await _audit.RecordAsync(userId.ToString(), "order.create", saved.Id.ToString(), $"total={saved.Total}", ct);
        return Mappings.ToDto(saved);
    }

    public async Task<OrderDto?> GetAsync(int id, CancellationToken ct = default)
    {
        var order = await _orders.GetByIdAsync(id, ct);
        return order is null ? null : Mappings.ToDto(order);
    }

    public async Task<PagedResult<OrderDto>> ListByCustomerAsync(int customerId, PageRequest page, CancellationToken ct = default)
    {
        var rows = await _orders.ListByCustomerAsync(customerId, page.Skip, page.Take, ct);
        var total = await _orders.CountByCustomerAsync(customerId, ct);
        return PagedResult<OrderDto>.Create(rows.Select(Mappings.ToDto).ToList(), page, total);
    }

    public async Task<bool> CancelAsync(int id, CancellationToken ct = default)
    {
        var order = await _orders.GetByIdAsync(id, ct);
        if (order is null || order.Status == "cancelled")
        {
            return false;
        }

        foreach (var line in order.Lines)
        {
            await _inventory.RestockAsync(line.ProductId, line.Quantity, ct);
        }

        await _orders.UpdateStatusAsync(id, "cancelled", ct);
        return true;
    }
}
