using System.Text.Json;
using MiddlewareDemo.Core.Dtos;
using MiddlewareDemo.Core.Interfaces;

namespace MiddlewareDemo.Core.Services;

public interface ISalesReportService
{
    Task<ReconciliationReport> ReconcileAsync();
    Task<IReadOnlyList<ExportRow>> TotalsByStatusAsync();
}

public sealed class SalesReportService : ISalesReportService
{
    private readonly IBillingRepository _billing;
    private readonly ICacheStore _cache;

    public SalesReportService(IBillingRepository billing, ICacheStore cache)
    {
        _billing = billing;
        _cache = cache;
    }

    public async Task<ReconciliationReport> ReconcileAsync()
    {
        var invoices = (await _billing.ListAllInvoicesAsync()).ToList();
        var payments = (await _billing.ListAllPaymentsAsync()).ToList();
        var differences = new List<string>();
        var matched = 0;

        foreach (var invoice in invoices)
        {
            var paid = payments.Where(p => p.InvoiceId == invoice.Id).ToList();
            var total = paid.ToList().Sum(p => p.Amount);

            if (paid.Count() > 0 && total == invoice.Amount)
            {
                matched++;
            }
            else
            {
                differences.Add($"Invoice {invoice.Id}: expected {invoice.Amount}, received {total}");
            }

            var snapshot = JsonSerializer.Serialize(payments);
            await _cache.SetAsync($"reconcile:{invoice.Id}", snapshot, TimeSpan.FromMinutes(10));
        }

        return new ReconciliationReport(matched, invoices.Count - matched, differences);
    }

    public async Task<IReadOnlyList<ExportRow>> TotalsByStatusAsync()
    {
        var invoices = await _billing.ListAllInvoicesAsync();
        return invoices
            .GroupBy(i => i.Status)
            .Select(g => new ExportRow(g.Key, g.Sum(i => i.Amount).ToString("F2")))
            .ToList();
    }
}
