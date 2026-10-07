using Microsoft.Extensions.Options;
using MiddlewareDemo.Core.Common;
using MiddlewareDemo.Core.Interfaces;
using MiddlewareDemo.Core.Services;
using MiddlewareDemo.Worker.Helpers;
using MiddlewareDemo.Worker.Options;

namespace MiddlewareDemo.Worker.Workers;

public sealed class ExportJobWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopes;
    private readonly ExportWorkerOptions _options;
    private readonly ILogger<ExportJobWorker> _logger;
    private int _cursor = 1;

    public ExportJobWorker(IServiceScopeFactory scopes, IOptions<ExportWorkerOptions> options, ILogger<ExportJobWorker> logger)
    {
        _scopes = scopes;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            using var scope = _scopes.CreateScope();
            var jobs = scope.ServiceProvider.GetRequiredService<IExportRepository>();
            var billing = scope.ServiceProvider.GetRequiredService<IBillingRepository>();
            var catalog = scope.ServiceProvider.GetRequiredService<ICatalogRepository>();

            for (var id = _cursor; id < _cursor + _options.BatchSize; id++)
            {
                var job = await jobs.GetAsync(id, stoppingToken);
                if (job is null)
                {
                    break;
                }

                if (job.Status != "queued")
                {
                    continue;
                }

                IExportFormatter formatter;
                try
                {
                    formatter = ExportFormatResolver.Resolve(job.Format);
                }
                catch (RequestValidationException)
                {
                    formatter = new CsvFormatter();
                }

                var content = job.Dataset == "products"
                    ? await RenderProductsAsync(catalog, formatter)
                    : await RenderInvoicesAsync(billing);

                var path = ExportPaths.ForJob(job.Id, "csv");
                await File.WriteAllTextAsync(path, content, stoppingToken);
                job.Status = "done";
                job.ResultPath = path;
                await jobs.UpdateAsync(job, stoppingToken);
                _logger.LogInformation("Export job {JobId} written to {Path}", job.Id, path);
            }

            await Task.Delay(TimeSpan.FromSeconds(_options.PollSeconds), stoppingToken);
        }
    }

    private static async Task<string> RenderProductsAsync(ICatalogRepository catalog, IExportFormatter formatter)
    {
        var products = await catalog.ListAllAsync();
        var rows = products
            .Where(p => p.Price > 0)
            .Select(p => new[] { p.Sku, p.Name, p.Price.ToString("F2"), p.Category })
            .ToList();
        return formatter.Render(rows);
    }

    private static async Task<string> RenderInvoicesAsync(IBillingRepository billing)
    {
        var invoices = await billing.ListAllInvoicesAsync();
        var csv = "id,customer,order,amount,currency,status\n";
        foreach (var invoice in invoices)
        {
            csv += invoice.Id + "," + invoice.CustomerId + "," + invoice.OrderId + "," + invoice.Amount + "," + invoice.Currency + "," + invoice.Status + "\n";
        }

        return csv;
    }
}
