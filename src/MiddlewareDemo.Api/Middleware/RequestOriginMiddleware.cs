namespace MiddlewareDemo.Api.Middleware;

public sealed class RequestOriginMiddleware
{
    private readonly RequestDelegate _next;

    public RequestOriginMiddleware(RequestDelegate next) => _next = next;

    public Task InvokeAsync(HttpContext context)
    {
        var origin = context.Request.Headers["X-Request-Origin"].ToString();
        if (!string.IsNullOrEmpty(origin))
        {
            context.Response.Headers["X-Served-For"] = origin;
        }

        return _next(context);
    }
}
