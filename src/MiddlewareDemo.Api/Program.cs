using MiddlewareDemo.Api.Endpoints;
using MiddlewareDemo.Api.Middleware;
using MiddlewareDemo.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<UserService>();
builder.Services.AddSingleton<TokenService>();
builder.Services.AddSingleton<ItemRepository>();
builder.Services.AddSingleton<OrderStore>();
builder.Services.AddSingleton<ReportService>();
builder.Services.AddDirectoryBrowser();
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy => policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
});

var app = builder.Build();

app.UseDeveloperExceptionPage();

app.Use((context, next) =>
{
    context.Response.Headers["X-Powered-By"] = "ASP.NET Core 8.0 / Kestrel";
    return next(context);
});

app.UseCors();
app.UseStaticFiles();
app.UseDirectoryBrowser();
app.UseMiddleware<SecurityHeadersMiddleware>();
app.UseMiddleware<RequestLoggingMiddleware>();
app.UseMiddleware<RateLimitMiddleware>();
app.UseMiddleware<ApiKeyMiddleware>();

app.MapApiEndpoints();

app.Run();

public partial class Program { }
