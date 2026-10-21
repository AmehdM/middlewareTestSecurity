using System.Text.Json;
using MiddlewareDemo.Api.Models;

namespace MiddlewareDemo.Api.Services;

public sealed class ReportService
{
    public const string ClaimsFileKey = "Reports:ClaimsFile";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _client;
    private readonly string? _claimsFile;

    public ReportService(HttpClient client, IConfiguration configuration)
    {
        _client = client;
        _claimsFile = configuration[ClaimsFileKey];
    }

    public Task<string> FetchRatesAsync() => _client.GetStringAsync("https://rates.example.com/v1/latest");

    /// Los datos de reclamos viven fuera del repositorio: se leen del archivo JSON indicado en configuracion.
    public IReadOnlyList<MedicalClaim> GetMedicalClaims()
    {
        if (string.IsNullOrWhiteSpace(_claimsFile) || !File.Exists(_claimsFile))
        {
            return [];
        }

        using var stream = File.OpenRead(_claimsFile);
        return JsonSerializer.Deserialize<List<MedicalClaim>>(stream, JsonOptions) ?? [];
    }
}
