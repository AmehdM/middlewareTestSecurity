using Microsoft.Extensions.DependencyInjection;
using MiddlewareDemo.Core.Services;

namespace MiddlewareDemo.Core;

public static class DependencyInjection
{
    public static IServiceCollection AddCoreServices(this IServiceCollection services)
    {
        services.AddSingleton<IPasswordService, PasswordService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IAccountService, AccountService>();
        services.AddScoped<IPasswordResetService, PasswordResetService>();
        services.AddScoped<IApiClientService, ApiClientService>();
        services.AddScoped<ICustomerService, CustomerService>();
        services.AddScoped<ICatalogService, CatalogService>();
        services.AddScoped<IInventoryService, InventoryService>();
        services.AddScoped<IOrderService, OrderService>();
        services.AddScoped<IPrescriptionService, PrescriptionService>();
        services.AddScoped<IBillingService, BillingService>();
        services.AddScoped<ISalesReportService, SalesReportService>();
        services.AddScoped<IExportService, ExportService>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<IFileService, FileService>();
        services.AddScoped<IAuditService, AuditService>();
        services.AddScoped<ITenantService, TenantService>();
        return services;
    }
}
