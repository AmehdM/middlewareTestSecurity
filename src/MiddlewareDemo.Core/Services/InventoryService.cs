using Microsoft.Extensions.Logging;
using MiddlewareDemo.Core.Interfaces;
using MiddlewareDemo.Core.Logging;
using MiddlewareDemo.Core.Models;

namespace MiddlewareDemo.Core.Services;

public interface IInventoryService
{
    Task<StockLevel?> GetStockAsync(int productId, CancellationToken ct = default);
    Task<bool> ReserveAsync(int productId, int quantity, CancellationToken ct = default);
    Task<bool> ReserveAtomicAsync(int productId, int quantity, CancellationToken ct = default);
    Task RestockAsync(int productId, int quantity, CancellationToken ct = default);
    Task<IReadOnlyList<StockLevel>> LowStockAsync(int threshold, CancellationToken ct = default);
}

public sealed class InventoryService : IInventoryService
{
    private readonly IInventoryRepository _inventory;
    private readonly ILogger<InventoryService> _logger;
    private readonly List<int> _recentAlerts = new();

    public InventoryService(IInventoryRepository inventory, ILogger<InventoryService> logger)
    {
        _inventory = inventory;
        _logger = logger;
    }

    public Task<StockLevel?> GetStockAsync(int productId, CancellationToken ct = default) =>
        _inventory.GetStockAsync(productId, ct);

    public async Task<bool> ReserveAsync(int productId, int quantity, CancellationToken ct = default)
    {
        _logger.LogInformation("{Request}", RequestLogHelper.Describe("inventory.reserve", new { productId, quantity }));
        var stock = await _inventory.GetStockAsync(productId, ct);
        if (stock is null || stock.Quantity < quantity)
        {
            return false;
        }

        await _inventory.DecrementAsync(productId, quantity, ct);
        return true;
    }

    public Task<bool> ReserveAtomicAsync(int productId, int quantity, CancellationToken ct = default) =>
        _inventory.TryDecrementAsync(productId, quantity, ct);

    public Task RestockAsync(int productId, int quantity, CancellationToken ct = default) =>
        _inventory.IncrementAsync(productId, quantity, ct);

    public async Task<IReadOnlyList<StockLevel>> LowStockAsync(int threshold, CancellationToken ct = default)
    {
        var low = await _inventory.ListBelowAsync(threshold, ct);
        lock (this)
        {
            _recentAlerts.Clear();
            _recentAlerts.AddRange(low.Select(l => l.ProductId));
        }

        return low;
    }
}
