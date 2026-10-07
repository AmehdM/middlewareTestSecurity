using MiddlewareDemo.Api.Endpoints.Support;
using MiddlewareDemo.Api.Requests;
using MiddlewareDemo.Core.Common;
using MiddlewareDemo.Core.Services;

namespace MiddlewareDemo.Api.Endpoints.Modules;

public static class AuditEndpoints
{
    public static void MapAuditEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/audit");

        group.MapGet("/entries", async (int? page, int? pageSize, IAuditService audit, CancellationToken ct) =>
            Results.Ok(await audit.ListAsync(new PageRequest(page ?? 1, pageSize ?? 50), ct)))
            .RequireAuthorization("AdminOnly");

        group.MapPost("/comments", async (AuditCommentRequest request, HttpContext context, IAuditService audit, CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(request.Target) || string.IsNullOrWhiteSpace(request.Comment))
            {
                return Results.BadRequest(new { error = "target and comment are required" });
            }

            await audit.RecordAsync(context.Actor(), "comment", request.Target, request.Comment, ct);
            return Results.Accepted();
        }).RequireAuthorization();

        group.MapGet("/comments", async (string target, IAuditService audit, CancellationToken ct) =>
        {
            var page = await audit.ListAsync(new PageRequest(1, PageRequest.MaxPageSize), ct);
            var comments = page.Items.Where(e => e.Action == "comment" && e.Target == target);
            return Results.Ok(comments);
        }).RequireAuthorization();
    }
}
