using System.Net;
using Microsoft.AspNetCore.HttpOverrides;
using MiddlewareDemo.Api.Endpoints;
using MiddlewareDemo.Api.Middleware;
using MiddlewareDemo.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(options =>
{
    options.AddServerHeader = false;
    options.Limits.MaxRequestBodySize = 64 * 1024;
});

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost;
    options.ForwardLimit = 1;

    foreach (var entry in (builder.Configuration["ForwardedHeaders:KnownNetworks"] ?? "")
                 .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
    {
        var parts = entry.Split('/');
        if (parts.Length == 2 && IPAddress.TryParse(parts[0], out var address) && int.TryParse(parts[1], out var prefix))
        {
            options.KnownNetworks.Add(new Microsoft.AspNetCore.HttpOverrides.IPNetwork(address, prefix));
        }
    }

    foreach (var proxy in (builder.Configuration["ForwardedHeaders:KnownProxies"] ?? "")
                 .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
    {
        if (IPAddress.TryParse(proxy, out var address))
        {
            options.KnownProxies.Add(address);
        }
    }
});

builder.Services.AddSingleton<UserService>();
builder.Services.AddSingleton<TokenService>();
builder.Services.AddSingleton<ItemRepository>();
builder.Services.AddSingleton<OrderStore>();
builder.Services.AddHttpClient<ReportService>(client => client.Timeout = TimeSpan.FromSeconds(10));
builder.Services.AddMemoryCache(options => options.SizeLimit = 1000);
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        var origin = builder.Configuration["Cors:AllowedOrigin"];
        if (string.IsNullOrWhiteSpace(origin) || origin.Contains('*'))
        {
            throw new InvalidOperationException("Configuration 'Cors:AllowedOrigin' must be a single explicit origin.");
        }

        policy.WithOrigins(origin)
            .WithMethods("GET", "POST")
            .WithHeaders("Content-Type", "Authorization", ApiKeyMiddleware.HeaderName);
    });
});

var app = builder.Build();

if (string.IsNullOrWhiteSpace(app.Configuration[ApiKeyMiddleware.ConfigKey]))
{
    throw new InvalidOperationException($"Missing required configuration '{ApiKeyMiddleware.ConfigKey}'.");
}

if ((app.Configuration[TokenService.ConfigKey] ?? "").Length < TokenService.MinKeyLength)
{
    throw new InvalidOperationException(
        $"Configuration '{TokenService.ConfigKey}' must have at least {TokenService.MinKeyLength} characters.");
}

app.UseForwardedHeaders();
app.UseMiddleware<SecurityHeadersMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler(errorApp => errorApp.Run(context =>
    {
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        return context.Response.WriteAsJsonAsync(new { error = "internal_error" });
    }));
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseCors();
app.UseMiddleware<RequestLoggingMiddleware>();
app.UseMiddleware<RateLimitMiddleware>();
app.UseMiddleware<ApiKeyMiddleware>();

app.MapApiEndpoints();

app.Run();

public partial class Program { }
