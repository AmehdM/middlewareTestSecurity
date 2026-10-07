using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MiddlewareDemo.Core.Interfaces;
using MiddlewareDemo.Infrastructure.Caching;
using MiddlewareDemo.Infrastructure.Clients;
using MiddlewareDemo.Infrastructure.Data;
using MiddlewareDemo.Infrastructure.Repositories;
using MiddlewareDemo.Infrastructure.Security;
using MiddlewareDemo.Infrastructure.Storage;

namespace MiddlewareDemo.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.TryAddSingleton(configuration);
        services.AddSingleton<SqliteDb>();
        services.AddMemoryCache(options => options.SizeLimit = 2048);
        services.AddSingleton<ICacheStore, MemoryCacheStore>();

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<CustomerRepository>();
        services.AddScoped<ICustomerRepository>(sp => new CachedCustomerRepository(sp.GetRequiredService<CustomerRepository>(), sp.GetRequiredService<ICacheStore>()));
        services.AddScoped<CatalogRepository>();
        services.AddScoped<ICatalogRepository>(sp => new CachedCatalogRepository(sp.GetRequiredService<CatalogRepository>()));
        services.AddScoped<InventoryRepository>();
        services.AddScoped<IInventoryRepository>(sp => sp.GetRequiredService<InventoryRepository>());
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<IPrescriptionRepository, PrescriptionRepository>();
        services.AddScoped<IBillingRepository, BillingRepository>();
        services.AddScoped<IExportRepository, ExportRepository>();
        services.AddScoped<INotificationRepository, NotificationRepository>();
        services.AddScoped<IFileRepository, FileRepository>();
        services.AddScoped<IAuditRepository, AuditRepository>();
        services.AddScoped<ITenantRepository, TenantRepository>();
        services.AddScoped<OpsDataRepository>();

        services.AddSingleton<ICryptoService, AesCryptoService>();
        services.AddSingleton<IKeyProvider, EnvKeyProvider>();
        services.AddSingleton<AesGcmCryptoService>();
        services.AddSingleton<LegacySigner>();
        services.AddScoped<ITokenIssuer, JwtTokenIssuer>();
        services.AddScoped<ApiKeyStore>();
        services.AddScoped<IApiKeyStore>(sp => sp.GetRequiredService<ApiKeyStore>());

        services.AddSingleton<LocalFileStorage>();
        services.AddSingleton<IFileStorage>(sp => sp.GetRequiredService<LocalFileStorage>());
        services.AddSingleton<ArchiveImporter>();
        services.AddSingleton<SafeArchiveExtractor>();
        services.AddSingleton<SupplierCatalogImporter>();
        services.AddSingleton<PriceListReader>();

        services.AddSingleton<IRatesClient, ExternalRatesClient>();
        services.AddSingleton<IMailSender, OutboxMailSender>();
        services.AddSingleton<ISmsSender, LogSmsSender>();
        services.AddSingleton<WebhookFetcher>();
        services.AddHttpClient<ShippingClient>(client =>
        {
            client.BaseAddress = new Uri(configuration["Integrations:ShippingUrl"] ?? "https://api.supplier-lab.example/shipping/");
            client.Timeout = TimeSpan.FromSeconds(10);
        });
        services.AddHttpClient<AllowlistedClient>(client => client.Timeout = TimeSpan.FromSeconds(10));

        return services;
    }
}
