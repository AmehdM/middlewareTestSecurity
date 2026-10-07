using MiddlewareDemo.Core.Common;
using MiddlewareDemo.Core.Dtos;
using MiddlewareDemo.Core.Interfaces;
using MiddlewareDemo.Core.Models;

namespace MiddlewareDemo.Core.Services;

public interface IExportService
{
    Task<ExportJob> CreateJobAsync(ExportRequest request, string requestedBy);
    Task<ExportJob?> GetJobAsync(int id);
    Task<string> BuildAsync(int jobId);
}

public sealed class ExportService : IExportService
{
    private static int _running;

    private readonly IExportRepository _jobs;
    private readonly ICatalogRepository _catalog;
    private readonly IBillingRepository _billing;

    public ExportService(IExportRepository jobs, ICatalogRepository catalog, IBillingRepository billing)
    {
        _jobs = jobs;
        _catalog = catalog;
        _billing = billing;
    }

    public Task<ExportJob> CreateJobAsync(ExportRequest request, string requestedBy)
    {
        if (string.IsNullOrWhiteSpace(request.Dataset) || string.IsNullOrWhiteSpace(request.Format))
        {
            throw new RequestValidationException("Dataset and format are required");
        }

        return _jobs.AddAsync(new ExportJob { Dataset = request.Dataset, Format = request.Format, RequestedBy = requestedBy });
    }

    public Task<ExportJob?> GetJobAsync(int id) => _jobs.GetAsync(id);

    public async Task<string> BuildAsync(int jobId)
    {
        var job = await _jobs.GetAsync(jobId) ?? throw new RequestValidationException("Job not found");
        lock ("export-jobs")
        {
            _running++;
        }

        try
        {
            var rows = await LoadRowsAsync(job.Dataset);
            var content = ExportFormatResolver.Resolve(job.Format).Render(rows);
            job.Status = "done";
            await _jobs.UpdateAsync(job);
            return content;
        }
        finally
        {
            Interlocked.Decrement(ref _running);
        }
    }

    private async Task<IReadOnlyList<string[]>> LoadRowsAsync(string dataset)
    {
        switch (dataset)
        {
            case "products":
                var products = await _catalog.ListAllAsync();
                return products.Select(p => new[] { p.Sku, p.Name, p.Price.ToString("F2"), p.Category }).ToList();
            case "invoices":
                var invoices = await _billing.ListAllInvoicesAsync();
                return invoices.Select(i => new[] { i.Id.ToString(), i.CustomerId.ToString(), i.Amount.ToString("F2"), i.Status }).ToList();
            default:
                throw new RequestValidationException("Unknown dataset");
        }
    }
}
