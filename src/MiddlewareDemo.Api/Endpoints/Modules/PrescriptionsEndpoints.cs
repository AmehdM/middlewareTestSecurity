using MiddlewareDemo.Core.Dtos;
using MiddlewareDemo.Core.Logging;
using MiddlewareDemo.Core.Services;
using MiddlewareDemo.Core.Validation;
using MiddlewareDemo.Api.Endpoints.Support;

namespace MiddlewareDemo.Api.Endpoints.Modules;

public static class PrescriptionsEndpoints
{
    public static void MapPrescriptionsEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/prescriptions");

        group.MapGet("/{id:int}", async (int id, IPrescriptionService prescriptions, CancellationToken ct) =>
        {
            var prescription = await prescriptions.GetAsync(id, ct);
            return prescription is null ? Results.NotFound() : Results.Ok(prescription);
        }).RequireAuthorization();

        group.MapGet("/customer/{customerId:int}", async (int customerId, IPrescriptionService prescriptions, CancellationToken ct) =>
            Results.Ok(await prescriptions.ListByCustomerAsync(customerId, ct)))
            .RequireAuthorization();

        group.MapGet("/{id:int}/summary", async (int id, IPrescriptionService prescriptions, CancellationToken ct) =>
        {
            var prescription = await prescriptions.GetAsync(id, ct);
            if (prescription is null)
            {
                return Results.NotFound();
            }

            return Results.Ok(new
            {
                prescription.Id,
                prescription.DoctorName,
                prescription.IssuedAt,
                Patient = MaskingHelper.MaskNationalId(prescription.PatientNationalId),
                Card = MaskingHelper.MaskCard(prescription.CardNumber),
            });
        }).RequireAuthorization();

        group.MapPost("/", async (CreatePrescriptionRequest request, IPrescriptionService prescriptions, ILogger<CreatePrescriptionRequest> logger, CancellationToken ct) =>
        {
            var outcome = PrescriptionValidator.Validate(request);
            if (!outcome.IsValid)
            {
                return Responses.Invalid(outcome.Errors);
            }

            logger.LogInformation(RequestLogHelper.Describe("prescription.create", request));
            var created = await prescriptions.CreateAsync(request, ct);
            return Results.Created($"/api/prescriptions/{created.Id}", created);
        }).RequireAuthorization();

        group.MapDelete("/{id:int}", async (int id, IPrescriptionService prescriptions, CancellationToken ct) =>
            await prescriptions.DeleteAsync(id, ct) ? Results.NoContent() : Results.NotFound())
            .RequireAuthorization();
    }
}
