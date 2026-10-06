using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace MiddlewareDemo.Tests;

public sealed class ApiTests : IClassFixture<ApiTests.ApiFactory>
{
    private const string Admin = "admin@lab.local";
    private const string Ana = "ana.lopez@lab.local";
    private const string Carlos = "carlos.ruiz@lab.local";

    private readonly ApiFactory _factory;

    public ApiTests(ApiFactory factory) => _factory = factory;

    [Fact]
    public async Task Health_WithoutKey_Returns200()
    {
        var response = await _factory.CreateClient().GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Orders_WithoutKey_Returns401()
    {
        var response = await _factory.CreateClient().GetAsync("/api/orders");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Orders_WithWrongKey_Returns401()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Api-Key", "wrong-" + _factory.ApiKey);

        var response = await client.GetAsync("/api/orders");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Orders_WithKeyButNoToken_Returns401()
    {
        var response = await _factory.CreateKeyClient().GetAsync("/api/orders");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Orders_WithKeyAndToken_Returns200()
    {
        var client = await _factory.CreateUserClientAsync(Ana);

        var response = await client.GetAsync("/api/orders");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Orders_AreVisibleOnlyToTheirOwnerAndAdmin()
    {
        var ana = await _factory.CreateUserClientAsync(Ana);
        var customer = "Owner-" + Guid.NewGuid().ToString("N");
        var created = await ana.PostAsJsonAsync("/api/orders", new { customer, itemId = 1, quantity = 2 });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);

        var carlos = await _factory.CreateUserClientAsync(Carlos);
        var admin = await _factory.CreateUserClientAsync(Admin);

        Assert.Contains(customer, await ana.GetStringAsync("/api/orders"));
        Assert.DoesNotContain(customer, await carlos.GetStringAsync("/api/orders"));
        Assert.Contains(customer, await admin.GetStringAsync("/api/orders"));
    }

    [Fact]
    public async Task HealthcareClaims_WithoutKey_Returns401()
    {
        var response = await _factory.CreateClient().GetAsync("/api/reports/healthcare-claims");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task HealthcareClaims_WithKeyButNoToken_Returns401()
    {
        var response = await _factory.CreateKeyClient().GetAsync("/api/reports/healthcare-claims");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task HealthcareClaims_WithNonAdminToken_Returns403()
    {
        var client = await _factory.CreateUserClientAsync(Ana);

        var response = await client.GetAsync("/api/reports/healthcare-claims");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task HealthcareClaims_WithAdminToken_Returns200()
    {
        var client = await _factory.CreateUserClientAsync(Admin);

        var response = await client.GetAsync("/api/reports/healthcare-claims");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task CreateOrder_WithValidData_Returns201()
    {
        var client = await _factory.CreateUserClientAsync(Ana);

        var response = await client.PostAsJsonAsync("/api/orders", new { customer = "Acme", itemId = 1, quantity = 5 });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Theory]
    [InlineData("", 5)]
    [InlineData("   ", 5)]
    [InlineData("Acme", 0)]
    [InlineData("Acme", 1001)]
    public async Task CreateOrder_WithInvalidData_Returns400(string customer, int quantity)
    {
        var client = await _factory.CreateUserClientAsync(Ana);

        var response = await client.PostAsJsonAsync("/api/orders", new { customer, itemId = 1, quantity });

        Assert.Equal(400, (int)response.StatusCode);
    }

    [Fact]
    public async Task CreateOrder_WithLongCustomer_Returns400()
    {
        var client = await _factory.CreateUserClientAsync(Ana);

        var response = await client.PostAsJsonAsync("/api/orders", new { customer = new string('a', 81), itemId = 1, quantity = 5 });

        Assert.Equal(400, (int)response.StatusCode);
    }

    [Fact]
    public async Task Responses_IncludeSecurityHeaders()
    {
        var response = await _factory.CreateClient().GetAsync("/health");

        Assert.Equal("nosniff", response.Headers.GetValues("X-Content-Type-Options").Single());
        Assert.Equal("DENY", response.Headers.GetValues("X-Frame-Options").Single());
        Assert.Equal("no-referrer", response.Headers.GetValues("Referrer-Policy").Single());
        Assert.False(response.Headers.Contains("X-Powered-By"));
        Assert.Contains("no-store", response.Headers.CacheControl!.ToString());
    }

    [Fact]
    public async Task CreateItem_WithoutToken_Returns401()
    {
        var response = await _factory.CreateKeyClient().PostAsJsonAsync("/api/items", new { name = "Widget", price = 10 });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreateItem_WithNonAdminToken_Returns403()
    {
        var client = await _factory.CreateUserClientAsync(Ana);

        var response = await client.PostAsJsonAsync("/api/items", new { name = "Widget", price = 10 });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Files_WithoutToken_Returns401()
    {
        var response = await _factory.CreateKeyClient().GetAsync("/api/files/report.txt");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Files_WithNonAdminToken_Returns403()
    {
        var client = await _factory.CreateUserClientAsync(Ana);

        var response = await client.GetAsync("/api/files/report.txt");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Files_OutsideTheFilesFolder_Returns404ForAdmin()
    {
        var client = await _factory.CreateUserClientAsync(Admin);

        var response = await client.GetAsync("/api/files/..%2Fappsettings.json");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UserProfile_WithoutToken_Returns401()
    {
        var response = await _factory.CreateKeyClient().GetAsync("/api/users/2");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task UserProfile_OfAnotherUser_Returns403()
    {
        var client = await _factory.CreateUserClientAsync(Ana);

        var response = await client.GetAsync("/api/users/3");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task AdminToken_CanReadAdminListWithoutPasswordHashes()
    {
        var client = await _factory.CreateUserClientAsync(Admin);

        var response = await client.GetAsync("/api/admin/users");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.DoesNotContain("PasswordHash", body, StringComparison.OrdinalIgnoreCase);
    }

    public sealed class ApiFactory : WebApplicationFactory<Program>
    {
        private readonly Dictionary<string, Lazy<Task<string>>> _tokens = new();
        private readonly object _lock = new();

        public string ApiKey { get; } = Guid.NewGuid().ToString("N");
        public string JwtKey { get; } = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N");
        public string Password { get; } = Guid.NewGuid().ToString("N");

        public HttpClient CreateKeyClient()
        {
            var client = CreateClient();
            client.DefaultRequestHeaders.Add("X-Api-Key", ApiKey);
            return client;
        }

        public async Task<HttpClient> CreateUserClientAsync(string email)
        {
            Lazy<Task<string>> token;
            lock (_lock)
            {
                if (!_tokens.TryGetValue(email, out token!))
                {
                    token = new Lazy<Task<string>>(() => LoginAsync(email));
                    _tokens[email] = token;
                }
            }

            var client = CreateKeyClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await token.Value);
            return client;
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureAppConfiguration((_, config) =>
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Security:ApiKey"] = ApiKey,
                    ["Security:JwtKey"] = JwtKey,
                    ["Seed:AdminPassword"] = Password,
                    ["Seed:UserPassword"] = Password,
                }));
        }

        private async Task<string> LoginAsync(string email)
        {
            var response = await CreateKeyClient().PostAsJsonAsync("/api/auth/login", new { email, password = Password });
            response.EnsureSuccessStatusCode();
            return (await response.Content.ReadFromJsonAsync<Dictionary<string, string>>())!["token"];
        }
    }
}
