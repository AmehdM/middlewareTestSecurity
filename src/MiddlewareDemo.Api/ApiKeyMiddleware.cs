using System.Security.Cryptography;
using System.Text;

namespace MiddlewareDemo.Api;

public sealed class ApiKeyMiddleware
{
    public const string HeaderName = "X-Api-Key";
    public const string ConfigKey = "Security:ApiKey";

    private static readonly PathString HealthPath = new("/health");

    private readonly RequestDelegate _next;
    private readonly ILogger<ApiKeyMiddleware> _logger;
    private readonly byte[] _expectedHash;

    public ApiKeyMiddleware(RequestDelegate next, IConfiguration configuration, ILogger<ApiKeyMiddleware> logger)
    {
        var apiKey = configuration[ConfigKey];
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException(
                $"Missing required configuration '{ConfigKey}'. Set it with the environment variable 'Security__ApiKey' or with user-secrets.");
        }

        _next = next;
        _logger = logger;
        _expectedHash = Hash(apiKey);
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Path.Equals(HealthPath, StringComparison.OrdinalIgnoreCase)
            || HttpMethods.IsOptions(context.Request.Method))
        {
            await _next(context);
            return;
        }

        var provided = context.Request.Headers[HeaderName].ToString();
        var isValid = provided.Length > 0
            && CryptographicOperations.FixedTimeEquals(Hash(provided), _expectedHash);

        if (!isValid)
        {
            _logger.LogWarning("Rejected request to {Path}: missing or invalid API key.", context.Request.Path);
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new { error = "unauthorized" });
            return;
        }

        await _next(context);
    }

    private static byte[] Hash(string value) => SHA256.HashData(Encoding.UTF8.GetBytes(value));
}
