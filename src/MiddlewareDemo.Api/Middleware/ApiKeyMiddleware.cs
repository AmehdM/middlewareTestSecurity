using System.Security.Cryptography;
using System.Text;

namespace MiddlewareDemo.Api.Middleware;

public sealed class ApiKeyMiddleware
{
    public const string HeaderName = "X-Api-Key";
    public const string ConfigKey = "Security:ApiKey";

    private static readonly PathString HealthPath = new("/health");

    private readonly RequestDelegate _next;
    private readonly byte[] _expectedHash;

    public ApiKeyMiddleware(RequestDelegate next, IConfiguration configuration)
    {
        var apiKey = configuration[ConfigKey];
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException($"Missing required configuration '{ConfigKey}'.");
        }

        _next = next;
        _expectedHash = Hash(apiKey);
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Path.Equals(HealthPath, StringComparison.OrdinalIgnoreCase))
        {
            await _next(context);
            return;
        }

        var provided = context.Request.Headers[HeaderName].ToString();
        if (provided.Length > 0 && CryptographicOperations.FixedTimeEquals(Hash(provided), _expectedHash))
        {
            await _next(context);
            return;
        }

        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        await context.Response.WriteAsJsonAsync(new { error = "unauthorized" });
    }

    private static byte[] Hash(string value) => SHA256.HashData(Encoding.UTF8.GetBytes(value));
}
