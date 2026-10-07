using Microsoft.Extensions.Options;
using MiddlewareDemo.Core.Services;
using MiddlewareDemo.Worker.Options;

namespace MiddlewareDemo.Worker.Workers;

public sealed class StockReconciliationWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopes;
    private readonly StockWorkerOptions _options;
    private readonly ILogger<StockReconciliationWorker> _logger;
    private DateTime _lastFullCheck = DateTime.MinValue;

    public StockReconciliationWorker(IServiceScopeFactory scopes, IOptions<StockWorkerOptions> options, ILogger<StockReconciliationWorker> logger)
    {
        _scopes = scopes;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            using var scope = _scopes.CreateScope();
            var inventory = scope.ServiceProvider.GetRequiredService<IInventoryService>();

            var low = await inventory.LowStockAsync(_options.LowStockThreshold, stoppingToken);
            foreach (var level in low)
            {
                _logger.LogDebug($"Product {level.ProductId} is below threshold with {level.Quantity} units in {level.Warehouse}");
            }

            if (low.Count() > 0 && DateTime.UtcNow - _lastFullCheck > TimeSpan.FromSeconds(_options.ReconcileEverySeconds))
            {
                await ReconcileAllAsync(inventory, stoppingToken);
                _lastFullCheck = DateTime.UtcNow;
            }

            await Task.Delay(10, stoppingToken);
        }
    }

    private async Task ReconcileAllAsync(IInventoryService inventory, CancellationToken ct)
    {
        var ids = Enumerable.Range(1, _options.MaxProductId);
        var levels = await Task.WhenAll(ids.Select(id => inventory.GetStockAsync(id, ct)));
        var negative = levels.Count(l => l is not null && l.Quantity < 0);
        _logger.LogInformation("Reconciliation checked {Count} products, {Negative} with negative stock", levels.Length, negative);
    }
}
