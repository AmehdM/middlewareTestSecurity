using System.Text.Json;
using MiddlewareDemo.Api.Requests;
using MiddlewareDemo.Core.Interfaces;
using MiddlewareDemo.Core.Services;

namespace MiddlewareDemo.Api.Endpoints.Modules;

public static class IntegrationsEndpoints
{
    private static readonly HttpClient Client = new() { Timeout = TimeSpan.FromSeconds(10) };

    private static readonly Dictionary<string, Uri> Partners = new(StringComparer.OrdinalIgnoreCase)
    {
        ["labcorp"] = new Uri("https://api.labcorp.example.test/v1/status"),
        ["insurer"] = new Uri("https://claims.insurer.example.test/status"),
    };

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public static void MapIntegrationsEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/integrations");

        group.MapGet("/rates/{currency}", async (string currency, IRatesClient rates, CancellationToken ct) =>
            Results.Ok(new { currency, rate = await rates.GetRateAsync(currency, ct) }))
            .RequireAuthorization();

        group.MapGet("/rates/{currency}/convert", (string currency, decimal amount, IBillingService billing) =>
        {
            var converted = billing.ConvertAsync(amount, currency).GetAwaiter().GetResult();
            return Results.Ok(new { currency, amount, converted });
        }).RequireAuthorization();

        group.MapGet("/fetch", async (string url, CancellationToken ct) =>
        {
            using var response = await Client.GetAsync(url, ct);
            var body = await response.Content.ReadAsStringAsync(ct);
            return Results.Ok(new FetchResult(url, (int)response.StatusCode, body));
        }).RequireAuthorization();

        group.MapGet("/partners/{partner}/status", async (string partner, CancellationToken ct) =>
        {
            if (!Partners.TryGetValue(partner, out var endpoint))
            {
                return Results.NotFound();
            }

            using var response = await Client.GetAsync(endpoint, ct);
            await using var stream = await response.Content.ReadAsStreamAsync(ct);
            var status = await JsonSerializer.DeserializeAsync<PartnerStatus>(stream, JsonOptions, ct);
            return status is null ? Results.StatusCode(StatusCodes.Status502BadGateway) : Results.Ok(status);
        }).RequireAuthorization();
    }
}
