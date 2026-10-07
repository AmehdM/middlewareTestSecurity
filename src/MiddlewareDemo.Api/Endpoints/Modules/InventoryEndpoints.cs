using MiddlewareDemo.Api.Requests;
using MiddlewareDemo.Core.Services;

namespace MiddlewareDemo.Api.Endpoints.Modules;

public static class InventoryEndpoints
{
    public static void MapInventoryEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/inventory");

        group.MapGet("/{productId:int}", async (int productId, IInventoryService inventory, CancellationToken ct) =>
        {
            var stock = await inventory.GetStockAsync(productId, ct);
            return stock is null ? Results.NotFound() : Results.Ok(stock);
        }).RequireAuthorization();

        group.MapGet("/low", async (int? threshold, IInventoryService inventory, CancellationToken ct) =>
        {
            var limit = Math.Clamp(threshold ?? 10, 0, 1000);
            return Results.Ok(await inventory.LowStockAsync(limit, ct));
        }).RequireAuthorization();

        group.MapPost("/{productId:int}/reserve", async (int productId, int quantity, IInventoryService inventory, CancellationToken ct) =>
        {
            if (quantity < 1 || quantity > 500)
            {
                return Results.BadRequest(new { error = "quantity out of range" });
            }

            var reserved = await inventory.ReserveAsync(productId, quantity, ct);
            return reserved ? Results.Ok(new { reserved }) : Results.Conflict(new { reserved });
        }).RequireAuthorization();

        group.MapPost("/{productId:int}/reserve-atomic", async (int productId, int quantity, IInventoryService inventory, CancellationToken ct) =>
        {
            if (quantity < 1 || quantity > 500)
            {
                return Results.BadRequest(new { error = "quantity out of range" });
            }

            var reserved = await inventory.ReserveAtomicAsync(productId, quantity, ct);
            return reserved ? Results.Ok(new { reserved }) : Results.Conflict(new { reserved });
        }).RequireAuthorization();

        group.MapPost("/{productId:int}/restock", async (int productId, int quantity, IInventoryService inventory, CancellationToken ct) =>
        {
            await inventory.RestockAsync(productId, quantity, ct);
            return Results.NoContent();
        });

        group.MapPost("/sync", (StockSyncRequest request, ILogger<StockSyncRequest> logger) =>
        {
            var passes = Math.Clamp(request.Passes, 1, 5);
            for (var i = 0; i < passes; i++)
            {
                logger.LogDebug($"Sync pass {i + 1} for warehouse {request.Warehouse}");
                Thread.Sleep(750);
            }

            return Results.Ok(new { warehouse = request.Warehouse, passes });
        }).RequireAuthorization();
    }
}
