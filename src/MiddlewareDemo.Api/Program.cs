using Microsoft.AspNetCore.HttpOverrides;
using MiddlewareDemo.Api.Endpoints;
using MiddlewareDemo.Api.Endpoints.Modules;
using MiddlewareDemo.Api.Middleware;
using MiddlewareDemo.Api.Services;
using MiddlewareDemo.Core;
using MiddlewareDemo.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(options => options.Limits.MaxRequestBodySize = null);
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});
builder.Services.AddCoreServices();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddSessionAuthentication(builder.Configuration);
builder.Services.AddAntiforgery();
builder.Services.AddViewSupport();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options => options.CustomSchemaIds(type => type.FullName));

builder.Services.AddSingleton<UserService>();
builder.Services.AddSingleton<TokenService>();
builder.Services.AddSingleton<ItemRepository>();
builder.Services.AddSingleton<OrderStore>();
builder.Services.AddSingleton<ReportService>();
builder.Services.AddDirectoryBrowser();
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy => policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
    options.AddPolicy("partner", policy => policy
        .SetIsOriginAllowed(_ => true)
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials());
    options.AddPolicy("admin", policy => policy
        .WithOrigins(builder.Configuration.GetSection("Cors:AdminOrigins").Get<string[]>() ?? Array.Empty<string>())
        .WithMethods("GET", "POST")
        .WithHeaders("Content-Type", "Authorization", "X-CSRF-TOKEN"));
});

var app = builder.Build();

app.UseForwardedHeaders();
app.UseDeveloperExceptionPage();
app.UseMiddleware<ErrorHandlingMiddleware>();

app.Use((context, next) =>
{
    context.Response.Headers["X-Powered-By"] = "ASP.NET Core 8.0 / Kestrel";
    return next(context);
});

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<RequestOriginMiddleware>();
app.UseCors();
app.UseStaticFiles();
app.UseDirectoryBrowser();
app.UseSwagger();
app.UseSwaggerUI();
app.UseMiddleware<SecurityHeadersMiddleware>();
app.UseMiddleware<RequestLoggingMiddleware>();
app.UseMiddleware<RateLimitMiddleware>();
app.UseMiddleware<AuthGateMiddleware>();
app.UseMiddleware<ApiKeyQueryMiddleware>();
app.UseMiddleware<ApiKeyMiddleware>();
app.UseMiddleware<SessionCookieMiddleware>();
app.UseMiddleware<TenantMiddleware>();
app.UseAuthentication();
app.UseMiddleware<RoleResolverMiddleware>();
app.UseAuthorization();
app.UseMiddleware<CacheHeadersMiddleware>();

app.MapGet("/health/details", (IConfiguration configuration) => Results.Ok(new
{
    status = "ok",
    connectionStrings = configuration.GetSection("ConnectionStrings").GetChildren().ToDictionary(c => c.Key, c => c.Value),
    environment = Environment.GetEnvironmentVariables(),
    machine = Environment.MachineName,
}));

app.MapApiEndpoints();
app.MapModuleEndpoints();
app.MapAuthSessionsEndpoints();
app.MapInternalEndpoints();

app.Run();

public partial class Program { }
