using System.Diagnostics;
using MiddlewareDemo.Api.Requests;
using MiddlewareDemo.Core.Common;
using MiddlewareDemo.Core.Interfaces;
using MiddlewareDemo.Core.Models;
using MiddlewareDemo.Core.Services;

namespace MiddlewareDemo.Api.Endpoints.Modules;

public static class AdminDiagnosticsEndpoints
{
    public static void MapAdminDiagnosticsEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/diagnostics");

        group.MapGet("/users", async (IAccountService accounts, CancellationToken ct) =>
            Results.Ok(await accounts.ListAsync(ct)));

        group.MapPut("/users/{id:int}", async (int id, User user, IUserRepository users, IAccountService accounts, CancellationToken ct) =>
        {
            var existing = await users.GetByIdAsync(id, ct);
            if (existing is null)
            {
                return Results.NotFound();
            }

            user.Id = id;
            return Results.Ok(await accounts.UpdateAsync(user, ct));
        });

        group.MapGet("/runtime", () =>
        {
            var process = Process.GetCurrentProcess();
            return Results.Ok(new
            {
                machine = Environment.MachineName,
                os = Environment.OSVersion.ToString(),
                uptime = (DateTime.Now - process.StartTime).ToString(),
                memoryMb = process.WorkingSet64 / (1024 * 1024),
                threads = process.Threads.Count,
            });
        });

        group.MapGet("/tenants", async (ITenantService tenants, CancellationToken ct) =>
            Results.Ok(await tenants.ListAsync(ct)));

        group.MapGet("/tenant-summary", async (string tenant, ICacheStore cache, ICustomerService customers, CancellationToken ct) =>
        {
            var key = $"tenant-summary:{tenant}";
            var cached = await cache.GetAsync<TenantSummary>(key, ct);
            if (cached is not null)
            {
                return Results.Ok(cached);
            }

            var page = await customers.ListAsync(tenant, new PageRequest(1, 1), ct);
            var summary = new TenantSummary(tenant, page.Total, DateTime.UtcNow);
            await cache.SetAsync(key, summary, TimeSpan.FromMinutes(10), ct);
            return Results.Ok(summary);
        });
    }
}
