using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Caching.Memory;
using MiddlewareDemo.Api.Models;
using MiddlewareDemo.Api.Services;

namespace MiddlewareDemo.Api.Endpoints;

public static class ApiEndpoints
{
    public const string AdminPolicy = "Admin";

    private const int MaxNameLength = 100;
    private const int MaxDescriptionLength = 500;
    private const int MaxPageSize = 100;

    private static readonly string FilesRoot = Path.Combine(AppContext.BaseDirectory, "files");

    private static readonly Regex SkuPattern =
        new(@"^[a-z]+[A-Z][a-z]+$", RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(100));

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

    private static IResult Invalid(string field, string message) =>
        Results.ValidationProblem(new Dictionary<string, string[]> { [field] = [message] });

    private static int? GetUserId(ClaimsPrincipal principal) =>
        int.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;

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
            users.CreateResetToken(request.Email);
            return Results.Ok(new { message = "If the account exists, a reset message was sent." });
        });
    }

    private static void MapUsers(WebApplication app)
    {
        app.MapGet("/api/users/{id:int}", (int id, ClaimsPrincipal principal, UserService users) =>
        {
            var isOwner = GetUserId(principal) == id;
            if (!isOwner && !principal.IsInRole("admin"))
            {
                return Results.StatusCode(StatusCodes.Status403Forbidden);
            }

            var user = users.Find(id);
            return user is null
                ? Results.NotFound()
                : Results.Ok(new UserProfile(user.Id, user.Email, user.NationalId, user.Salary));
        }).RequireAuthorization();

        app.MapGet("/api/admin/users", (UserService users) =>
            Results.Ok(users.All().Select(u => new AdminUserView(u.Id, u.Email, u.Role))))
            .RequireAuthorization(AdminPolicy);
    }

    private static void MapItems(WebApplication app)
    {
        app.MapGet("/api/items", async (ItemRepository items, int page = 1, int pageSize = 20) =>
        {
            if (page < 1 || pageSize < 1 || pageSize > MaxPageSize)
            {
                return Invalid("paging", $"page must be at least 1 and pageSize between 1 and {MaxPageSize}.");
            }

            return Results.Ok(await items.GetPageAsync(page, pageSize));
        }).RequireAuthorization();

        app.MapGet("/api/items/search", async (string name, ItemRepository items, IMemoryCache cache) =>
        {
            if (string.IsNullOrWhiteSpace(name) || name.Length > MaxNameLength)
            {
                return Invalid("name", $"Name is required and must be at most {MaxNameLength} characters.");
            }

            var result = await cache.GetOrCreateAsync($"search:{name}", async entry =>
            {
                entry.Size = 1;
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1);
                return await items.SearchAsync(name);
            });

            return Results.Ok(result);
        }).RequireAuthorization();

        app.MapGet("/api/items/validate-sku", (string? value) =>
        {
            if (string.IsNullOrEmpty(value) || value.Length > 64)
            {
                return Invalid("value", "Value is required and must be at most 64 characters.");
            }

            try
            {
                return Results.Ok(new { valid = SkuPattern.IsMatch(value) });
            }
            catch (RegexMatchTimeoutException)
            {
                return Results.Ok(new { valid = false });
            }
        }).RequireAuthorization();

        app.MapPost("/api/items", async (CreateItemRequest request, ItemRepository items) =>
        {
            var name = request.Name?.Trim();
            if (string.IsNullOrEmpty(name) || name.Length > MaxNameLength)
            {
                return Invalid("name", $"Name is required and must be at most {MaxNameLength} characters.");
            }

            if (!double.IsFinite(request.Price) || request.Price < 0 || request.Price > 1_000_000)
            {
                return Invalid("price", "Price must be between 0 and 1000000.");
            }

            var item = await items.AddAsync(name, request.Price);
            return Results.Created($"/api/items/{item.Id}", item);
        }).RequireAuthorization(AdminPolicy);

        app.MapPost("/api/import", (ImportRequest request) =>
        {
            var name = request.Name?.Trim();
            if (string.IsNullOrEmpty(name) || name.Length > MaxNameLength)
            {
                return Invalid("name", $"Name is required and must be at most {MaxNameLength} characters.");
            }

            if (request.Description is { Length: > MaxDescriptionLength })
            {
                return Invalid("description", $"Description must be at most {MaxDescriptionLength} characters.");
            }

            return Results.Ok(new { received = name });
        }).RequireAuthorization(AdminPolicy);
    }

    private static void MapOrders(WebApplication app)
    {
        app.MapGet("/api/orders", (ClaimsPrincipal principal, OrderStore orders) =>
            Results.Ok(VisibleOrders(principal, orders)))
            .RequireAuthorization();

        app.MapPost("/api/orders", (CreateOrderRequest request, ClaimsPrincipal principal, OrderStore orders) =>
        {
            var ownerId = GetUserId(principal);
            if (ownerId is null)
            {
                return Results.Unauthorized();
            }

            var customer = request.Customer?.Trim();
            if (string.IsNullOrEmpty(customer) || customer.Length > 80)
            {
                return Invalid("customer", "Customer is required and must be at most 80 characters.");
            }

            if (request.Quantity < 1 || request.Quantity > 1000)
            {
                return Invalid("quantity", "Quantity must be between 1 and 1000.");
            }

            var order = orders.Add(ownerId.Value, customer, request.ItemId, request.Quantity);
            return Results.Created("/api/orders", order);
        }).RequireAuthorization();

        app.MapGet("/api/orders/summary", async (ClaimsPrincipal principal, OrderStore orders, ItemRepository items) =>
        {
            var all = VisibleOrders(principal, orders);
            var catalog = await items.GetByIdsAsync(all.Select(o => o.ItemId));

            var lines = all.Select(order =>
            {
                catalog.TryGetValue(order.ItemId, out var item);
                return new { order.Id, order.Customer, Item = item?.Name, Total = (item?.Price ?? 0) * order.Quantity };
            });

            return Results.Ok(lines);
        }).RequireAuthorization();
    }

    private static IReadOnlyList<Order> VisibleOrders(ClaimsPrincipal principal, OrderStore orders)
    {
        if (principal.IsInRole("admin"))
        {
            return orders.All();
        }

        var userId = GetUserId(principal);
        return userId is null ? [] : orders.ForOwner(userId.Value);
    }

    private static void MapReports(WebApplication app)
    {
        app.MapGet("/api/reports/healthcare-claims", (ReportService reports) =>
            Results.Ok(reports.GetMedicalClaims()))
            .RequireAuthorization(AdminPolicy);

        app.MapGet("/api/reports/exchange-rates", async (ReportService reports) =>
            Results.Ok(new { rates = await reports.FetchRatesAsync() }))
            .RequireAuthorization();
    }

    private static void MapFiles(WebApplication app)
    {
        app.MapGet("/api/files/{**name}", (string name) =>
        {
            var root = Path.GetFullPath(FilesRoot) + Path.DirectorySeparatorChar;
            var full = Path.GetFullPath(Path.Combine(FilesRoot, name));
            var comparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

            if (!full.StartsWith(root, comparison) || !File.Exists(full))
            {
                return Results.NotFound();
            }

            var stream = new FileStream(full, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.Asynchronous | FileOptions.SequentialScan);
            return Results.File(stream, "application/octet-stream", Path.GetFileName(full));
        }).RequireAuthorization(AdminPolicy);
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
