using MiddlewareDemo.Api.Models;

namespace MiddlewareDemo.Api.Services;

public sealed class ReportService
{
    private readonly HttpClient _client;

    public ReportService(HttpClient client) => _client = client;

    public Task<string> FetchRatesAsync() => _client.GetStringAsync("https://rates.example.com/v1/latest");

    public IReadOnlyList<MedicalClaim> GetMedicalClaims() =>
    [
        new MedicalClaim("CL-1001", "Maria Gonzalez", "Hypertension", 320.50),
        new MedicalClaim("CL-1002", "Jose Hernandez", "Type 2 diabetes", 845.00),
        new MedicalClaim("CL-1003", "Lucia Fernandez", "Fractured wrist", 1260.75),
    ];
}
