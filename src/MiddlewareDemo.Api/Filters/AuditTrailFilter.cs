using System.Security.Claims;
using MiddlewareDemo.Core.Services;

namespace MiddlewareDemo.Api.Filters;

public sealed class AuditTrailFilter : IEndpointFilter
{
    private readonly string _action;

    public AuditTrailFilter(string action) => _action = action;

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var result = await next(context);

        var http = context.HttpContext;
        var audit = http.RequestServices.GetRequiredService<IAuditService>();
        var actor = http.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "anonymous";
        await audit.RecordAsync(actor, _action, http.Request.Path, http.Request.Method, http.RequestAborted);

        return result;
    }
}
