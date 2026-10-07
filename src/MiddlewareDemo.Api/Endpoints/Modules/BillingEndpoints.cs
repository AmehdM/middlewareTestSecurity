using MiddlewareDemo.Api.Endpoints.Support;
using MiddlewareDemo.Api.Requests;
using MiddlewareDemo.Api.Validation;
using MiddlewareDemo.Core.Dtos;
using MiddlewareDemo.Core.Services;

namespace MiddlewareDemo.Api.Endpoints.Modules;

public static class BillingEndpoints
{
    public static void MapBillingEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/billing");

        group.MapGet("/invoices", async (int customerId, IBillingService billing, CancellationToken ct) =>
            Results.Ok(await billing.ListInvoicesAsync(customerId, ct)))
            .RequireAuthorization();

        group.MapGet("/invoices/{id:int}", async (int id, IBillingService billing, CancellationToken ct) =>
        {
            var invoice = await billing.GetInvoiceAsync(id, ct);
            return invoice is null ? Results.NotFound() : Results.Ok(invoice);
        }).RequireAuthorization();

        group.MapGet("/invoices/{id:int}/mine", async (int id, HttpContext context, IBillingService billing, ICustomerService customers, CancellationToken ct) =>
        {
            var invoice = await billing.GetInvoiceAsync(id, ct);
            if (invoice is null)
            {
                return Results.NotFound();
            }

            var owner = await customers.GetAsync(invoice.CustomerId, context.UserId(), context.IsAdmin(), ct);
            return owner is null ? Results.Forbid() : Results.Ok(invoice);
        }).RequireAuthorization();

        group.MapPost("/invoices", async (InvoicePayload payload, IBillingService billing, CancellationToken ct) =>
        {
            var errors = InvoicePayloadValidator.Validate(payload);
            if (errors.Count > 0)
            {
                return Responses.Invalid(errors);
            }

            var created = await billing.CreateInvoiceAsync(
                new CreateInvoiceRequest(payload.CustomerId, payload.OrderId, payload.Amount, payload.Currency!), ct);
            return Results.Created($"/api/billing/invoices/{created.Id}", created);
        }).RequireAuthorization();

        group.MapPost("/pay", async (PaymentRequest request, IBillingService billing, CancellationToken ct) =>
        {
            var payment = await billing.PayAsync(request, ct);
            return payment is null ? Results.NotFound() : Results.Ok(payment);
        }).RequireAuthorization();

        group.MapGet("/convert", async (decimal amount, string currency, IBillingService billing, CancellationToken ct) =>
        {
            if (!ValidationPatterns.IsCurrency(currency))
            {
                return Results.BadRequest(new { error = "invalid currency" });
            }

            return Results.Ok(new { amount, currency, converted = await billing.ConvertAsync(amount, currency, ct) });
        });
    }
}
