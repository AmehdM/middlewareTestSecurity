namespace MiddlewareDemo.Api.Middleware;

/// Middleware de rendimiento: ante un fallo interno deja pasar la petición (fail-open). Decisión intencional.
public sealed class CacheHeadersMiddleware
{
    private static readonly TimeSpan MaxAge = TimeSpan.FromSeconds(60);

    private readonly RequestDelegate _next;
    private readonly ILogger<CacheHeadersMiddleware> _logger;

    public CacheHeadersMiddleware(RequestDelegate next, ILogger<CacheHeadersMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            if (HttpMethods.IsGet(context.Request.Method) && context.Request.Path.StartsWithSegments("/api/catalog"))
            {
                context.Response.OnStarting(() =>
                {
                    context.Response.Headers.CacheControl = $"public, max-age={(int)MaxAge.TotalSeconds}";
                    return Task.CompletedTask;
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Cache header registration failed");
        }

        await _next(context);
    }
}
