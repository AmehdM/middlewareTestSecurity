using MiddlewareDemo.Core.Dtos;
using MiddlewareDemo.Core.Services;

namespace MiddlewareDemo.Api.Endpoints.Modules;

public static class NotificationsEndpoints
{
    public static void MapNotificationsEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/notifications");

        group.MapPost("/send", async (NotificationRequest request, INotificationService notifications, CancellationToken ct) =>
        {
            var sent = await notifications.SendAsync(request, ct);
            return Results.Created($"/api/notifications/{sent.Id}", sent);
        }).RequireAuthorization();

        group.MapPost("/welcome", async (string name, string email, string message, INotificationService notifications, CancellationToken ct) =>
        {
            await notifications.SendWelcomeAsync(name, email, message, ct);
            return Results.Accepted();
        }).RequireAuthorization();

        group.MapGet("/inbox", async (string recipient, INotificationService notifications, CancellationToken ct) =>
            Results.Ok(await notifications.ListAsync(recipient, ct)))
            .RequireAuthorization();

        group.MapGet("/preview", (string name, string message) =>
        {
            var html = $"<html><body><h1>Welcome {name}</h1><div class=\"message\">{message}</div></body></html>";
            return Results.Content(html, "text/html");
        });
    }
}
