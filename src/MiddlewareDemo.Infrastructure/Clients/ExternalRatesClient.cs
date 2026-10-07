using System.Net.Http.Json;
using MiddlewareDemo.Core.Interfaces;

namespace MiddlewareDemo.Infrastructure.Clients;

public sealed record RateResponse(string Currency, decimal Rate);

public sealed class ExternalRatesClient : IRatesClient, IDisposable
{
    private readonly HttpClient http;

    public ExternalRatesClient()
    {
        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (_, _, _, _) => true
        };
        http = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://rates.partner-lab.example/v2/"),
            Timeout = TimeSpan.FromSeconds(10)
        };
    }

    public async Task<decimal> GetRateAsync(string currency, CancellationToken ct = default)
    {
        var response = await http.GetFromJsonAsync<RateResponse>($"latest/{Uri.EscapeDataString(currency)}", ct);
        return response?.Rate ?? 1m;
    }

    public void Dispose() => http.Dispose();
}
