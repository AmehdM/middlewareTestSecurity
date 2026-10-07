using MiddlewareDemo.Core;
using MiddlewareDemo.Infrastructure;
using MiddlewareDemo.Worker.Options;
using MiddlewareDemo.Worker.Workers;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddCoreServices();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.Configure<StockWorkerOptions>(builder.Configuration.GetSection(StockWorkerOptions.Section));
builder.Services.Configure<ExportWorkerOptions>(builder.Configuration.GetSection(ExportWorkerOptions.Section));
builder.Services.Configure<NotificationWorkerOptions>(builder.Configuration.GetSection(NotificationWorkerOptions.Section));
builder.Services.Configure<FileProcessingOptions>(builder.Configuration.GetSection(FileProcessingOptions.Section));
builder.Services.Configure<AuditPurgeOptions>(builder.Configuration.GetSection(AuditPurgeOptions.Section));
builder.Services.Configure<RatesOptions>(builder.Configuration.GetSection(RatesOptions.Section));

builder.Services.AddHostedService<StockReconciliationWorker>();
builder.Services.AddHostedService<ExportJobWorker>();
builder.Services.AddHostedService<InvoiceDigestWorker>();
builder.Services.AddHostedService<NotificationDispatchWorker>();
builder.Services.AddHostedService<FileProcessingWorker>();
builder.Services.AddHostedService<AuditPurgeWorker>();
builder.Services.AddHostedService<RatesRefreshWorker>();

await builder.Build().RunAsync();
