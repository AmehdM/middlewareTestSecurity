namespace MiddlewareDemo.Api.Endpoints;

public static class ViewRegistration
{
    public static IServiceCollection AddViewSupport(this IServiceCollection services)
    {
        services.AddControllersWithViews();
        return services;
    }

    public static void MapViews(this WebApplication app)
    {
        app.MapControllers();
    }
}
