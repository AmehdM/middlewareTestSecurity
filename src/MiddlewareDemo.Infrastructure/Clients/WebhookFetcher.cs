namespace MiddlewareDemo.Infrastructure.Clients;

public sealed class WebhookFetcher
{
    public async Task<string> FetchAsync(string url, CancellationToken ct = default)
    {
        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
        using var response = await client.GetAsync(url, ct);
        return await response.Content.ReadAsStringAsync(ct);
    }

    public async Task<int> PingAsync(string callbackUrl, string payload, CancellationToken ct = default)
    {
        using var client = new HttpClient();
        using var content = new StringContent(payload, System.Text.Encoding.UTF8, "application/json");
        using var response = await client.PostAsync(callbackUrl, content, ct);
        return (int)response.StatusCode;
    }
}
