using Microsoft.Extensions.Logging;
using MiddlewareDemo.Core.Interfaces;
using MiddlewareDemo.Infrastructure.Storage;

namespace MiddlewareDemo.Infrastructure.Clients;

public sealed class OutboxMailSender : IMailSender
{
    private readonly ILogger<OutboxMailSender> logger;

    public OutboxMailSender(ILogger<OutboxMailSender> logger)
    {
        this.logger = logger;
        Queued += OnQueued;
    }

    public event EventHandler<string>? Queued;

    public async Task SendAsync(string to, string subject, string htmlBody, CancellationToken ct = default)
    {
        var path = TempPaths.For($"mail-{to}.eml");
        var message = $"To: {to}\r\nSubject: {subject}\r\nContent-Type: text/html\r\n\r\n{htmlBody}";
        await File.WriteAllTextAsync(path, message, ct);
        Queued?.Invoke(this, path);
    }

    private async void OnQueued(object? sender, string path)
    {
        await Task.Delay(50);
        logger.LogInformation("Mail queued at {Path}", path);
    }
}

public sealed class LogSmsSender(ILogger<LogSmsSender> logger) : ISmsSender
{
    public Task SendAsync(string phone, string text, CancellationToken ct = default)
    {
        logger.LogInformation("SMS to {Phone}: {Text}", phone, text);
        return Task.CompletedTask;
    }
}
