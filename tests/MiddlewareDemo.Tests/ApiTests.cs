using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace MiddlewareDemo.Tests;

public sealed class ApiTests : IClassFixture<ApiTests.ApiFactory>
{
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
    public async Task Orders_WithCorrectKey_Returns200()
    {
        var response = await _factory.CreateAuthorizedClient().GetAsync("/api/orders");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task HealthcareClaims_WithoutKey_Returns401()
    {
        var response = await _factory.CreateClient().GetAsync("/api/reports/healthcare-claims");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreateOrder_WithValidData_Returns201()
    {
        var response = await _factory.CreateAuthorizedClient().PostAsJsonAsync("/api/orders", new { customer = "Acme", itemId = 1, quantity = 5 });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Theory]
    [InlineData("", 5)]
    [InlineData("   ", 5)]
    [InlineData("Acme", 0)]
    [InlineData("Acme", 1001)]
    public async Task CreateOrder_WithInvalidData_Returns400(string customer, int quantity)
    {
        var response = await _factory.CreateAuthorizedClient().PostAsJsonAsync("/api/orders", new { customer, itemId = 1, quantity });

        Assert.Equal(400, (int)response.StatusCode);
    }

    [Fact]
    public async Task CreateOrder_WithLongCustomer_Returns400()
    {
        var response = await _factory.CreateAuthorizedClient().PostAsJsonAsync("/api/orders", new { customer = new string('a', 81), itemId = 1, quantity = 5 });

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
    }

    [Fact]
    public async Task UserProfile_WithoutToken_Returns401()
    {
        var response = await _factory.CreateAuthorizedClient().GetAsync("/api/users/2");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task AdminToken_CanReadAdminListWithoutPasswordHashes()
    {
        var client = _factory.CreateAuthorizedClient();
        var login = await client.PostAsJsonAsync("/api/auth/login", new { email = "admin@lab.local", password = _factory.AdminPassword });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);

        var token = (await login.Content.ReadFromJsonAsync<Dictionary<string, string>>())!["token"];
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync("/api/admin/users");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.DoesNotContain("PasswordHash", body, StringComparison.OrdinalIgnoreCase);
    }

    public sealed class ApiFactory : WebApplicationFactory<Program>
    {
        public string ApiKey { get; } = Guid.NewGuid().ToString("N");
        public string JwtKey { get; } = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N");
        public string AdminPassword { get; } = Guid.NewGuid().ToString("N");

        public HttpClient CreateAuthorizedClient()
        {
            var client = CreateClient();
            client.DefaultRequestHeaders.Add("X-Api-Key", ApiKey);
            return client;
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureAppConfiguration((_, config) =>
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Security:ApiKey"] = ApiKey,
                    ["Security:JwtKey"] = JwtKey,
                    ["Seed:AdminPassword"] = AdminPassword,
                }));
        }
    }
}
