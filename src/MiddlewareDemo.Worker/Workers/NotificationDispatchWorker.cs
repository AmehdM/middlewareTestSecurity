using Microsoft.Extensions.Options;
using MiddlewareDemo.Core.Dtos;
using MiddlewareDemo.Core.Interfaces;
using MiddlewareDemo.Core.Logging;
using MiddlewareDemo.Core.Services;
using MiddlewareDemo.Worker.Options;

namespace MiddlewareDemo.Worker.Workers;

public sealed class NotificationDispatchWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopes;
    private readonly NotificationWorkerOptions _options;
    private readonly ILogger<NotificationDispatchWorker> _logger;

    public NotificationDispatchWorker(IServiceScopeFactory scopes, IOptions<NotificationWorkerOptions> options, ILogger<NotificationDispatchWorker> logger)
    {
        _scopes = scopes;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(_options.IntervalMinutes));
        do
        {
            await DispatchAsync(stoppingToken);
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task DispatchAsync(CancellationToken ct)
    {
        using var scope = _scopes.CreateScope();
        var customers = scope.ServiceProvider.GetRequiredService<ICustomerRepository>();
        var prescriptions = scope.ServiceProvider.GetRequiredService<IPrescriptionRepository>();
        var notifications = scope.ServiceProvider.GetRequiredService<INotificationService>();

        var skip = 0;
        while (true)
        {
            var page = await customers.ListAsync(_options.Tenant, skip, _options.PageSize, ct);
            if (page.Count == 0)
            {
                break;
            }

            foreach (var customer in page)
            {
                var message = $"Hello {customer.Name}, {_options.WelcomeText}";
                await notifications.SendWelcomeAsync(customer.Name, customer.Email, message, ct);
                _logger.LogInformation("Welcome dispatched to " + customer.Name + " <" + customer.Email + ">");

                foreach (var prescription in await prescriptions.ListByCustomerAsync(customer.Id, ct))
                {
                    _logger.LogInformation("Refill reminder for {Name} national id {NationalId} card {Card}",
                        customer.Name, customer.NationalId, prescription.CardNumber);
                    await notifications.SendAsync(new NotificationRequest(customer.Phone, "Refill reminder", message, "sms"), ct);
                }

                _logger.LogInformation("Reminders queued for customer {Id} ({Masked})",
                    customer.Id, MaskingHelper.MaskNationalId(customer.NationalId));
            }

            skip += page.Count;
        }
    }
}
