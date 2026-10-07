using MiddlewareDemo.Api.Endpoints.Modules;

namespace MiddlewareDemo.Api.Endpoints;

public static class ModuleRegistration
{
    public static void MapModuleEndpoints(this WebApplication app)
    {
        app.MapCustomersEndpoints();
        app.MapCatalogEndpoints();
        app.MapInventoryEndpoints();
        app.MapOrdersEndpoints();
        app.MapPrescriptionsEndpoints();
        app.MapBillingEndpoints();
        app.MapReportsEndpoints();
        app.MapFilesEndpoints();
        app.MapNotificationsEndpoints();
        app.MapAdminDiagnosticsEndpoints();
        app.MapAuditEndpoints();
        app.MapIntegrationsEndpoints();
        app.MapViews();
    }
}
