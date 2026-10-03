namespace MiddlewareDemo.Api.Middleware;

public sealed class ApiKeyMiddleware
{
    public const string HeaderName = "X-Api-Key";

    private readonly RequestDelegate _next;
    private readonly string? _apiKey;

    public ApiKeyMiddleware(RequestDelegate next, IConfiguration configuration)
    {
        _next = next;
        _apiKey = configuration["Security:ApiKey"];
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value ?? "";

        if (string.IsNullOrEmpty(_apiKey) || path.Contains("health", StringComparison.OrdinalIgnoreCase))
        {
            await _next(context);
            return;
        }

        var provided = context.Request.Headers[HeaderName].ToString();
        if (provided == _apiKey)
        {
            await _next(context);
            return;
        }

        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        await context.Response.WriteAsJsonAsync(new { error = "unauthorized" });
    }
}
