using MiddlewareDemo.Api.Endpoints.Support;
using MiddlewareDemo.Api.Requests;
using MiddlewareDemo.Core.Common;
using MiddlewareDemo.Core.Dtos;
using MiddlewareDemo.Core.Interfaces;
using MiddlewareDemo.Core.Models;
using MiddlewareDemo.Core.Services;

namespace MiddlewareDemo.Api.Endpoints.Modules;

public static class CustomersEndpoints
{
    public static void MapCustomersEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/customers");

        group.MapGet("/", async (string tenantId, int? page, int? pageSize, ICustomerService customers, CancellationToken ct) =>
        {
            var request = new PageRequest(page ?? 1, pageSize ?? 25);
            return Results.Ok(await customers.ListAsync(tenantId, request, ct));
        }).RequireAuthorization();

        group.MapGet("/{id:int}", async (int id, HttpContext context, ICustomerService customers, CancellationToken ct) =>
        {
            var customer = await customers.GetAsync(id, context.UserId(), context.IsAdmin(), ct);
            return customer is null ? Results.NotFound() : Results.Ok(customer);
        }).RequireAuthorization();

        group.MapGet("/search", async (string tenantId, string term, ICustomerService customers, CancellationToken ct) =>
            Results.Ok(await customers.SearchAsync(tenantId, term, ct)));

        group.MapGet("/by-national-id/{nationalId}", async (string nationalId, string tenantId, ICustomerService customers, CancellationToken ct) =>
        {
            var matches = await customers.SearchAsync(tenantId, nationalId, ct);
            return matches.Count == 0 ? Results.NotFound() : Results.Ok(matches[0]);
        });

        group.MapPost("/", async (Customer customer, ICustomerRepository repository, CancellationToken ct) =>
        {
            var created = await repository.AddAsync(customer, ct);
            return Results.Created($"/api/customers/{created.Id}", created);
        });

        group.MapPut("/{id:int}", async (int id, UpdateCustomerRequest request, ICustomerService customers, CancellationToken ct) =>
        {
            var updated = await customers.UpdateAsync(id, request, ct);
            return updated is null ? Results.NotFound() : Results.Ok(updated);
        }).RequireAuthorization();

        group.MapDelete("/{id:int}", async (int id, ICustomerService customers, CancellationToken ct) =>
            await customers.DeleteAsync(id, ct) ? Results.NoContent() : Results.NotFound())
            .RequireAuthorization();

        group.MapPost("/{id:int}/notes", async (int id, CustomerNoteRequest request, HttpContext context, IAuditService audit, CancellationToken ct) =>
        {
            await audit.RecordAsync(context.Actor(), "customer.note", $"customer:{id}", request.Note ?? string.Empty, ct);
            return Results.Accepted();
        }).RequireAuthorization();
    }
}
