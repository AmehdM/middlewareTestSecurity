using Microsoft.Extensions.Configuration;

namespace MiddlewareDemo.Infrastructure.Clients;

public sealed class AllowlistedClient(HttpClient http, IConfiguration configuration)
{
    private readonly HashSet<string> allowed = new(
        (configuration["Integrations:AllowedHosts"] ?? "api.supplier-lab.example,rates.partner-lab.example")
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
        StringComparer.OrdinalIgnoreCase);

    public async Task<string> GetAsync(string url, CancellationToken ct = default)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps || !allowed.Contains(uri.Host))
        {
            throw new ArgumentException("Destination is not permitted.", nameof(url));
        }

        using var response = await http.GetAsync(uri, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync(ct);
    }
}
