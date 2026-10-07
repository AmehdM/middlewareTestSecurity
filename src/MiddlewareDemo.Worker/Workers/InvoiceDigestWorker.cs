using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using MiddlewareDemo.Core.Interfaces;
using MiddlewareDemo.Worker.Options;

namespace MiddlewareDemo.Worker.Workers;

public sealed class InvoiceDigestWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopes;
    private readonly ExportWorkerOptions _options;
    private readonly ILogger<InvoiceDigestWorker> _logger;

    public InvoiceDigestWorker(IServiceScopeFactory scopes, IOptions<ExportWorkerOptions> options, ILogger<InvoiceDigestWorker> logger)
    {
        _scopes = scopes;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(_options.DigestEveryMinutes));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await WriteDigestAsync(stoppingToken);
        }
    }

    private async Task WriteDigestAsync(CancellationToken ct)
    {
        using var scope = _scopes.CreateScope();
        var billing = scope.ServiceProvider.GetRequiredService<IBillingRepository>();
        var invoices = await billing.ListAllInvoicesAsync();

        var builder = new StringBuilder();
        builder.AppendLine("status,count,total");
        foreach (var group in invoices.GroupBy(i => i.Status).OrderBy(g => g.Key))
        {
            builder.Append(group.Key).Append(',').Append(group.Count()).Append(',').Append(group.Sum(i => i.Amount)).AppendLine();
        }

        Directory.CreateDirectory(_options.DigestDirectory);
        var name = $"digest-{DateTime.UtcNow:yyyyMMdd}-{Convert.ToHexString(RandomNumberGenerator.GetBytes(8))}.csv";
        var path = Path.Combine(_options.DigestDirectory, name);
        await File.WriteAllTextAsync(path, builder.ToString(), ct);
        _logger.LogInformation("Invoice digest written with {Count} invoices", invoices.Count);
    }
}
