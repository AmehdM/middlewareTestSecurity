using System.Diagnostics;
using System.Text;
using MiddlewareDemo.Api.Endpoints.Support;
using MiddlewareDemo.Api.Requests;
using MiddlewareDemo.Core.Dtos;
using MiddlewareDemo.Core.Interfaces;
using MiddlewareDemo.Core.Services;

namespace MiddlewareDemo.Api.Endpoints.Modules;

public static class ReportsEndpoints
{
    public static void MapReportsEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/reportcenter");

        group.MapGet("/reconcile", async (ISalesReportService reports) =>
            Results.Ok(await reports.ReconcileAsync()))
            .RequireAuthorization("AdminOnly");

        group.MapGet("/status-totals", async (ISalesReportService reports) =>
            Results.Ok(await reports.TotalsByStatusAsync()))
            .RequireAuthorization();

        group.MapGet("/invoices.csv", async (IBillingRepository billing) =>
        {
            var invoices = await billing.ListAllInvoicesAsync();
            var csv = "id,customer,order,amount,currency,status\n";
            foreach (var invoice in invoices)
            {
                csv += $"{invoice.Id},{invoice.CustomerId},{invoice.OrderId},{invoice.Amount},{invoice.Currency},{invoice.Status}\n";
            }

            return Results.Text(csv, "text/csv");
        }).RequireAuthorization();

        group.MapGet("/payments.csv", async (IBillingRepository billing, CancellationToken ct) =>
        {
            var payments = await billing.ListAllPaymentsAsync();
            var builder = new StringBuilder("id,invoice,amount,paid_at\n");
            foreach (var payment in payments.Take(5000))
            {
                ct.ThrowIfCancellationRequested();
                builder.Append(payment.Id).Append(',')
                    .Append(payment.InvoiceId).Append(',')
                    .Append(payment.Amount).Append(',')
                    .Append(payment.PaidAt.ToString("O")).Append('\n');
            }

            return Results.Text(builder.ToString(), "text/csv");
        }).RequireAuthorization("AdminOnly");

        group.MapPost("/export", async (ExportRequest request, HttpContext context, IExportService exports) =>
        {
            var job = await exports.CreateJobAsync(request, context.Actor());
            var content = await exports.BuildAsync(job.Id);
            return Results.Ok(new { job.Id, length = content.Length });
        }).RequireAuthorization();

        group.MapPost("/pdf", async (string source, ReportPdfRequest request, IWebHostEnvironment env) =>
        {
            try
            {
                var outputDir = Path.Combine(Path.GetTempPath(), "report-pdf");
                Directory.CreateDirectory(outputDir);
                var output = Path.Combine(outputDir, $"{Guid.NewGuid():N}.pdf");

                var info = new ProcessStartInfo("wkhtmltopdf")
                {
                    Arguments = $"--quiet --title \"{request.Title}\" {source} {output}",
                    RedirectStandardError = true,
                    UseShellExecute = false,
                };

                var exitCode = await Task.Run(() =>
                {
                    using var process = Process.Start(info)!;
                    process.WaitForExit();
                    return process.ExitCode;
                });

                return Results.Ok(new { output, exitCode });
            }
            catch (Exception ex)
            {
                return Results.Problem(ex.ToString());
            }
        }).RequireAuthorization();
    }
}
