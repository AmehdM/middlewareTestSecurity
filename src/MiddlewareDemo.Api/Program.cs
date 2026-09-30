using MiddlewareDemo.Api;

const int MaxNameLength = 50;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(options =>
{
    options.AddServerHeader = false;
    options.Limits.MaxRequestBodySize = 10 * 1024;
});

builder.Services.AddSingleton<ItemStore>();
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        var origin = builder.Configuration["Cors:AllowedOrigin"];
        if (string.IsNullOrWhiteSpace(origin) || origin.Contains('*'))
        {
            throw new InvalidOperationException(
                "Configuration 'Cors:AllowedOrigin' must be a single explicit origin (wildcards are not allowed).");
        }

        policy.WithOrigins(origin).WithMethods("GET", "POST").WithHeaders("Content-Type", ApiKeyMiddleware.HeaderName);
    });
});

var app = builder.Build();

if (string.IsNullOrWhiteSpace(app.Configuration[ApiKeyMiddleware.ConfigKey]))
{
    throw new InvalidOperationException(
        $"Missing required configuration '{ApiKeyMiddleware.ConfigKey}'. Set the environment variable 'Security__ApiKey' before starting the app.");
}

app.UseMiddleware<SecurityHeadersMiddleware>();

if (!app.Environment.IsDevelopment())
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
app.UseMiddleware<ApiKeyMiddleware>();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

var items = app.MapGroup("/api/items");

items.MapGet("/", (ItemStore store) => Results.Ok(store.GetAll()));

items.MapPost("/", (CreateItemRequest request, ItemStore store) =>
{
    var name = request.Name?.Trim();
    if (string.IsNullOrEmpty(name) || name.Length > MaxNameLength)
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["name"] = [$"Name is required and must be at most {MaxNameLength} characters."],
        });
    }

    var item = store.Add(name);
    return Results.Created("/api/items", item);
});

app.Run();

public partial class Program;
