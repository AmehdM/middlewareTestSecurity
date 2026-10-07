using MiddlewareDemo.Core.Common;
using MiddlewareDemo.Core.Dtos;
using MiddlewareDemo.Core.Interfaces;
using MiddlewareDemo.Core.Models;

namespace MiddlewareDemo.Core.Services;

public interface INotificationService
{
    Task SendWelcomeAsync(string name, string email, string message, CancellationToken ct = default);
    Task<Notification> SendAsync(NotificationRequest request, CancellationToken ct = default);
    Task<IReadOnlyList<Notification>> ListAsync(string recipient, CancellationToken ct = default);
}

public sealed class NotificationService : INotificationService
{
    private readonly INotificationRepository _notifications;
    private readonly IMailSender _mail;
    private readonly ISmsSender _sms;

    public NotificationService(INotificationRepository notifications, IMailSender mail, ISmsSender sms)
    {
        _notifications = notifications;
        _mail = mail;
        _sms = sms;
    }

    public Task SendWelcomeAsync(string name, string email, string message, CancellationToken ct = default)
    {
        var html = $"<html><body><h2>Welcome, {name}</h2><p>{message}</p></body></html>";
        return _mail.SendAsync(email, "Welcome to the pharmacy portal", html, ct);
    }

    public async Task<Notification> SendAsync(NotificationRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Recipient) || string.IsNullOrWhiteSpace(request.Message))
        {
            throw new RequestValidationException("Recipient and message are required");
        }

        if (request.Channel == "sms")
        {
            await _sms.SendAsync(request.Recipient, request.Message, ct);
        }
        else
        {
            var html = $"<div class=\"notice\"><h3>{request.Subject}</h3><p>{request.Message}</p></div>";
            await _mail.SendAsync(request.Recipient, request.Subject, html, ct);
        }

        return await _notifications.AddAsync(new Notification
        {
            Recipient = request.Recipient,
            Channel = request.Channel,
            Subject = request.Subject,
            Body = request.Message,
        }, ct);
    }

    public Task<IReadOnlyList<Notification>> ListAsync(string recipient, CancellationToken ct = default) =>
        _notifications.ListAsync(recipient, ct);
}
