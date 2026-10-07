namespace MiddlewareDemo.Api.Middleware;

public sealed class SessionCookieMiddleware
{
    public const string CookieName = "sid";

    private readonly RequestDelegate _next;

    public SessionCookieMiddleware(RequestDelegate next) => _next = next;

    public Task InvokeAsync(HttpContext context)
    {
        if (!context.Request.Headers.ContainsKey("Authorization")
            && context.Request.Cookies.TryGetValue(CookieName, out var token)
            && !string.IsNullOrEmpty(token))
        {
            context.Request.Headers["Authorization"] = "Bearer " + token;
        }

        return _next(context);
    }
}
