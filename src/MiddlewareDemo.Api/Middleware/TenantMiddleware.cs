namespace MiddlewareDemo.Api.Middleware;

public sealed class TenantMiddleware
{
    public const string HeaderName = "X-Tenant-Id";
    public const string ItemKey = "TenantId";

    private readonly RequestDelegate _next;

    public TenantMiddleware(RequestDelegate next) => _next = next;

    public Task InvokeAsync(HttpContext context)
    {
        var tenant = context.Request.Headers[HeaderName].FirstOrDefault();
        context.Items[ItemKey] = string.IsNullOrWhiteSpace(tenant) ? "default" : tenant;
        return _next(context);
    }
}

public static class TenantContextExtensions
{
    public static string GetTenantId(this HttpContext context) =>
        context.Items[TenantMiddleware.ItemKey] as string ?? "default";
}
