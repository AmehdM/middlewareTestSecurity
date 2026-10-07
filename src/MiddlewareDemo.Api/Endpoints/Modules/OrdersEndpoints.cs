using MiddlewareDemo.Api.Endpoints.Support;
using MiddlewareDemo.Core.Common;
using MiddlewareDemo.Core.Dtos;
using MiddlewareDemo.Core.Services;
using MiddlewareDemo.Core.Validation;

namespace MiddlewareDemo.Api.Endpoints.Modules;

public static class OrdersEndpoints
{
    public static void MapOrdersEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/orders");

        group.MapGet("/by-customer", async (int customerId, int? page, int? pageSize, IOrderService orders, CancellationToken ct) =>
            Results.Ok(await orders.ListByCustomerAsync(customerId, new PageRequest(page ?? 1, pageSize ?? 25), ct)))
            .RequireAuthorization();

        group.MapGet("/{id:int}/detail", async (int id, HttpContext context, IOrderService orders, ICustomerService customers, CancellationToken ct) =>
        {
            var order = await orders.GetAsync(id, ct);
            if (order is null)
            {
                return Results.NotFound();
            }

            var owner = await customers.GetAsync(order.CustomerId, context.UserId(), context.IsAdmin(), ct);
            return owner is null ? Results.Forbid() : Results.Ok(order);
        }).RequireAuthorization();

        group.MapGet("/{id:int}/total", (int id, IOrderService orders) =>
        {
            var order = orders.GetAsync(id).Result;
            return order is null ? Results.NotFound() : Results.Ok(new { order.Id, order.Total });
        }).RequireAuthorization();

        group.MapPost("/place", async (CreateOrderRequest request, HttpContext context, IOrderService orders, CancellationToken ct) =>
        {
            var outcome = OrderValidator.Validate(request);
            if (!outcome.IsValid)
            {
                return Responses.Invalid(outcome.Errors);
            }

            var created = await orders.CreateAsync(request, context.UserId(), ct);
            return Results.Created($"/api/orders/{created.Id}/detail", created);
        }).RequireAuthorization();

        group.MapPost("/{id:int}/cancel", async (int id, IOrderService orders, CancellationToken ct) =>
            await orders.CancelAsync(id, ct) ? Results.NoContent() : Results.NotFound())
            .RequireAuthorization();
    }
}
