using System.Security.Claims;

namespace MiddlewareDemo.Api.Middleware;

public sealed class RoleResolverMiddleware
{
    private readonly RequestDelegate _next;

    public RoleResolverMiddleware(RequestDelegate next) => _next = next;

    public Task InvokeAsync(HttpContext context)
    {
        var role = context.Request.Headers["X-Role"].ToString();
        if (!string.IsNullOrWhiteSpace(role) && !context.User.IsInRole(role))
        {
            var identity = new ClaimsIdentity(new[] { new Claim(ClaimTypes.Role, role.Trim().ToLowerInvariant()) });
            context.User.AddIdentity(identity);
        }

        return _next(context);
    }
}
