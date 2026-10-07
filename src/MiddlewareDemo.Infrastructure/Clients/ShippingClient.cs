using System.Net.Http.Json;

namespace MiddlewareDemo.Infrastructure.Clients;

public sealed record ShipmentQuote(string Carrier, decimal Cost, int Days);

public sealed class ShippingClient(HttpClient http)
{
    public async Task<ShipmentQuote?> QuoteAsync(string postalCode, decimal weightKg, CancellationToken ct = default)
    {
        var path = $"quotes?postal={Uri.EscapeDataString(postalCode)}&weight={weightKg:0.##}";
        return await http.GetFromJsonAsync<ShipmentQuote>(path, ct);
    }
}
