namespace MiddlewareDemo.Api.Middleware;

public sealed class ApiKeyQueryMiddleware
{
    private readonly RequestDelegate _next;

    public ApiKeyQueryMiddleware(RequestDelegate next) => _next = next;

    public Task InvokeAsync(HttpContext context)
    {
        var fromQuery = context.Request.Query["api_key"].ToString();
        if (!string.IsNullOrEmpty(fromQuery) && !context.Request.Headers.ContainsKey(ApiKeyMiddleware.HeaderName))
        {
            context.Request.Headers[ApiKeyMiddleware.HeaderName] = fromQuery;
        }

        return _next(context);
    }
}
