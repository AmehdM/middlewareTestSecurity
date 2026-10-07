using System.Security.Claims;

namespace MiddlewareDemo.Api.Filters;

public sealed class OwnerOnlyFilter : IEndpointFilter
{
    private readonly string _routeKey;

    public OwnerOnlyFilter(string routeKey) => _routeKey = routeKey;

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var http = context.HttpContext;
        var subject = http.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? http.User.FindFirstValue("sub");
        var requested = http.Request.RouteValues[_routeKey]?.ToString();

        if (string.IsNullOrEmpty(subject) || !string.Equals(subject, requested, StringComparison.Ordinal))
        {
            return Results.Forbid();
        }

        return await next(context);
    }
}
