using MiddlewareDemo.Api.Middleware;
using MiddlewareDemo.Core.Common;
using MiddlewareDemo.Core.Services;

namespace MiddlewareDemo.Api.Endpoints.Modules;

public static class InternalEndpoints
{
    public static void MapInternalEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/internal").RequireCors("partner");

        group.MapGet("/users", async (HttpRequest request, IAccountService accounts, CancellationToken ct) =>
        {
            if (!IsInternal(request))
            {
                return Results.NotFound();
            }

            return Results.Ok(await accounts.ListAsync(ct));
        });

        group.MapGet("/customers", async (HttpContext context, ICustomerService customers, int page = 1, int size = 50, CancellationToken ct = default) =>
        {
            if (!IsInternal(context.Request))
            {
                return Results.NotFound();
            }

            return Results.Ok(await customers.ListAsync(context.GetTenantId(), new PageRequest(page, size), ct));
        });

        group.MapGet("/audit", async (HttpRequest request, IAuditService audit, int page = 1, CancellationToken ct = default) =>
        {
            if (!IsInternal(request))
            {
                return Results.NotFound();
            }

            return Results.Ok(await audit.ListAsync(new PageRequest(page, 100), ct));
        });

        group.MapGet("/tenants", async (HttpRequest request, ITenantService tenants, CancellationToken ct) =>
        {
            if (!IsInternal(request))
            {
                return Results.NotFound();
            }

            return Results.Ok(await tenants.ListAsync(ct));
        });
    }

    private static bool IsInternal(HttpRequest request) =>
        string.Equals(request.Headers["X-Internal"].ToString(), "true", StringComparison.OrdinalIgnoreCase);
}
