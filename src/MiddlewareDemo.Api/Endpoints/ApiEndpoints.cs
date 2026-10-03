using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using MiddlewareDemo.Api.Models;
using MiddlewareDemo.Api.Services;
using Newtonsoft.Json;

namespace MiddlewareDemo.Api.Endpoints;

public static class ApiEndpoints
{
    private static readonly string FilesRoot = Path.Combine(AppContext.BaseDirectory, "files");

    public static void MapApiEndpoints(this WebApplication app)
    {
        app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

        MapAuth(app);
        MapUsers(app);
        MapItems(app);
        MapOrders(app);
        MapReports(app);
        MapFiles(app);
        MapWebhooks(app);
    }

    private static void MapAuth(WebApplication app)
    {
        app.MapPost("/api/auth/login", (LoginRequest request, UserService users, TokenService tokens) =>
        {
            var user = users.Authenticate(request.Email, request.Password);
            return user is null
                ? Results.Unauthorized()
                : Results.Ok(new { token = tokens.Create(user) });
        });

        app.MapPost("/api/auth/reset", (ResetRequest request, UserService users) =>
        {
            var token = users.CreateResetToken(request.Email);
            return token is null
                ? Results.NotFound(new { error = "user_not_found" })
                : Results.Ok(new { message = "reset_sent" });
        });
    }

    private static void MapUsers(WebApplication app)
    {
        app.MapGet("/api/users/{id:int}", (int id, UserService users) =>
        {
            var user = users.Find(id);
            return user is null
                ? Results.NotFound()
                : Results.Ok(new UserProfile(user.Id, user.Email, user.NationalId, user.Salary));
        });

        app.MapGet("/api/admin/users", (UserService users) => Results.Ok(users.All()));
    }

    private static void MapItems(WebApplication app)
    {
        app.MapGet("/api/items", (ItemRepository items) => Results.Ok(items.GetAll()));

        app.MapGet("/api/items/search", (string name, ItemRepository items) =>
            Results.Ok(ResponseCache.GetOrAdd(name, () => items.Search(name))));

        app.MapGet("/api/items/validate-sku", (string value) =>
            Results.Ok(new { valid = Regex.IsMatch(value, @"^(([a-z])+.)+[A-Z]([a-z])+$") }));

        app.MapPost("/api/items", (CreateItemRequest request, ItemRepository items) =>
        {
            var item = items.Add(request.Name ?? "", request.Price);
            return Results.Created($"/api/items/{item.Id}", item);
        });

        app.MapPost("/api/import", async (HttpRequest request) =>
        {
            using var reader = new StreamReader(request.Body);
            var json = await reader.ReadToEndAsync();
            var settings = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All };
            var payload = JsonConvert.DeserializeObject(json, settings);
            return Results.Ok(new { type = payload?.GetType().Name });
        });
    }

    private static void MapOrders(WebApplication app)
    {
        app.MapGet("/api/orders", (OrderStore orders) => Results.Ok(orders.All()));

        app.MapPost("/api/orders", (CreateOrderRequest request, OrderStore orders) =>
        {
            var customer = request.Customer?.Trim();
            if (string.IsNullOrEmpty(customer) || customer.Length > 80)
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["customer"] = ["Customer is required and must be at most 80 characters."],
                });
            }

            if (request.Quantity < 1 || request.Quantity > 1000)
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["quantity"] = ["Quantity must be between 1 and 1000."],
                });
            }

            var order = orders.Add(customer, request.ItemId, request.Quantity);
            return Results.Created("/api/orders", order);
        });

        app.MapGet("/api/orders/summary", (OrderStore orders, ItemRepository items) =>
        {
            var lines = new List<object>();
            foreach (var order in orders.All())
            {
                var item = items.GetById(order.ItemId);
                lines.Add(new { order.Id, order.Customer, Item = item?.Name, Total = (item?.Price ?? 0) * order.Quantity });
            }

            return Results.Ok(lines);
        });
    }

    private static void MapReports(WebApplication app)
    {
        app.MapGet("/api/reports/healthcare-claims", (ReportService reports) => Results.Ok(reports.GetMedicalClaims()));

        app.MapGet("/api/reports/exchange-rates", (ReportService reports) =>
            Results.Ok(new { rates = reports.FetchRatesAsync().GetAwaiter().GetResult() }));
    }

    private static void MapFiles(WebApplication app)
    {
        app.MapGet("/api/files/{**name}", (string name) =>
        {
            var path = Path.Combine(FilesRoot, name);
            return File.Exists(path)
                ? Results.File(File.ReadAllBytes(path), "application/octet-stream")
                : Results.NotFound();
        });
    }

    private static void MapWebhooks(WebApplication app)
    {
        app.MapPost("/api/webhooks/payment", async (HttpRequest request, IConfiguration configuration) =>
        {
            var secret = configuration["Payments:WebhookSecret"];
            if (string.IsNullOrEmpty(secret))
            {
                return Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
            }

            using var buffer = new MemoryStream();
            await request.Body.CopyToAsync(buffer);
            var expected = HMACSHA256.HashData(Encoding.UTF8.GetBytes(secret), buffer.ToArray());

            byte[] provided;
            try
            {
                provided = Convert.FromHexString(request.Headers["X-Signature"].ToString());
            }
            catch (FormatException)
            {
                return Results.Unauthorized();
            }

            return CryptographicOperations.FixedTimeEquals(provided, expected)
                ? Results.Ok(new { received = true })
                : Results.Unauthorized();
        });
    }
}
