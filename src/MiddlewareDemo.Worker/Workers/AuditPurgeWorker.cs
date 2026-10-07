using System.Text;
using Microsoft.Extensions.Options;
using MiddlewareDemo.Core.Interfaces;
using MiddlewareDemo.Core.Models;
using MiddlewareDemo.Worker.Options;

namespace MiddlewareDemo.Worker.Workers;

public sealed class AuditPurgeWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopes;
    private readonly IConfiguration _configuration;
    private readonly AuditPurgeOptions _options;
    private readonly ILogger<AuditPurgeWorker> _logger;

    public AuditPurgeWorker(IServiceScopeFactory scopes, IConfiguration configuration, IOptions<AuditPurgeOptions> options, ILogger<AuditPurgeWorker> logger)
    {
        _scopes = scopes;
        _configuration = configuration;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromHours(_options.IntervalHours));
        do
        {
            try
            {
                await ArchiveExpiredAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Audit archive failed for {Connection}", _configuration.GetConnectionString("Default"));
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task ArchiveExpiredAsync(CancellationToken ct)
    {
        using var scope = _scopes.CreateScope();
        var audit = scope.ServiceProvider.GetRequiredService<IAuditRepository>();
        var cutoff = DateTime.UtcNow.AddDays(-_options.RetentionDays);
        var expired = new List<AuditEntry>();

        var total = await audit.CountAsync(ct);
        for (var skip = 0; skip < total; skip += _options.PageSize)
        {
            var page = await audit.ListAsync(skip, _options.PageSize, ct);
            expired.AddRange(page.Where(e => e.At < cutoff));
        }

        if (expired.Count() > 0)
        {
            Directory.CreateDirectory(_options.ArchiveDirectory);
            var lines = expired.Select(e => $"{e.Id},{e.At:O},{e.Actor},{e.Action},{e.Target}");
            var path = Path.Combine(_options.ArchiveDirectory, $"audit-{cutoff:yyyyMMdd}.csv");
            await File.WriteAllTextAsync(path, string.Join("\n", lines), Encoding.UTF8, ct);
        }

        _logger.LogInformation("Audit archive finished, {Count} expired entries", expired.Count);
    }
}
