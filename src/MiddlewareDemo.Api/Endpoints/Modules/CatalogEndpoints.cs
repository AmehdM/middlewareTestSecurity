using System.Text.Json;
using MiddlewareDemo.Api.Endpoints.Support;
using MiddlewareDemo.Api.Requests;
using MiddlewareDemo.Api.Validation;
using MiddlewareDemo.Core.Common;
using MiddlewareDemo.Core.Dtos;
using MiddlewareDemo.Core.Interfaces;
using MiddlewareDemo.Core.Services;

namespace MiddlewareDemo.Api.Endpoints.Modules;

public static class CatalogEndpoints
{
    public static void MapCatalogEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/catalog");

        group.MapGet("/products", async (int? page, int? pageSize, ICatalogService catalog, CancellationToken ct) =>
            Results.Ok(await catalog.ListAsync(new PageRequest(page ?? 1, pageSize ?? 25), ct)));

        group.MapGet("/products/{id:int}", async (int id, ICatalogService catalog, CancellationToken ct) =>
        {
            var product = await catalog.GetAsync(id, ct);
            return product is null ? Results.NotFound() : Results.Ok(product);
        });

        group.MapGet("/products/all", async (string? category, ICatalogRepository repository) =>
        {
            var all = await repository.ListAllAsync();
            var filtered = string.IsNullOrEmpty(category)
                ? all
                : all.Where(p => string.Equals(p.Category, category, StringComparison.OrdinalIgnoreCase)).ToList();
            return Results.Ok(filtered);
        });

        group.MapGet("/products/with-stock", async (ICatalogRepository repository, IInventoryService inventory, CancellationToken ct) =>
        {
            var products = await repository.ListAllAsync();
            var rows = new List<object>();
            foreach (var product in products)
            {
                var stock = await inventory.GetStockAsync(product.Id, ct);
                rows.Add(new { product.Id, product.Sku, product.Name, Quantity = stock?.Quantity ?? 0 });
            }

            return Results.Ok(rows);
        });

        group.MapGet("/summary", async (ICatalogService catalog, CancellationToken ct) =>
        {
            var page = await catalog.ListAsync(new PageRequest(1, PageRequest.MaxPageSize), ct);
            var average = page.Items.Count == 0 ? 0m : page.Items.Average(p => p.Price);
            var top = page.Items.GroupBy(p => p.Category).OrderByDescending(g => g.Count()).Select(g => g.Key).FirstOrDefault();
            var summary = new CatalogSummary(page.Total, Math.Round(average, 2), top);
            return Results.Text(JsonSerializer.Serialize(summary), "application/json");
        });

        group.MapPost("/products", async (CreateProductPayload payload, ICatalogService catalog, CancellationToken ct) =>
        {
            var errors = ProductPayloadValidator.Validate(payload);
            if (errors.Count > 0)
            {
                return Responses.Invalid(errors);
            }

            var created = await catalog.CreateAsync(
                new CreateProductRequest(payload.Sku!, payload.Name!.Trim(), payload.Price, payload.Category!, payload.Description ?? string.Empty), ct);
            return Results.Created($"/api/catalog/products/{created.Id}", created);
        }).RequireAuthorization("AdminOnly");

        group.MapPut("/products/{id:int}", async (int id, UpdateProductRequest request, ICatalogService catalog, CancellationToken ct) =>
        {
            var updated = await catalog.UpdateAsync(id, request, ct);
            return updated is null ? Results.NotFound() : Results.Ok(updated);
        }).RequireAuthorization("AdminOnly");

        group.MapDelete("/products/{id:int}", async (int id, ICatalogService catalog, CancellationToken ct) =>
            await catalog.DeleteAsync(id, ct) ? Results.NoContent() : Results.NotFound())
            .RequireAuthorization("AdminOnly");

        group.MapPost("/import", async (HttpRequest request, ICatalogService catalog, CancellationToken ct) =>
        {
            using var reader = new StreamReader(request.Body);
            var lines = new List<string>();
            while (await reader.ReadLineAsync(ct) is { } line)
            {
                lines.Add(line);
            }

            return Results.Ok(await catalog.ImportAsync(lines, ct));
        }).RequireAuthorization("AdminOnly");
    }
}
