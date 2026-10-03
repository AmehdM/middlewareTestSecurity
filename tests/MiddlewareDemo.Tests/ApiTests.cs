using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace MiddlewareDemo.Tests;

public sealed class ApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly string _apiKey;

    public ApiTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _apiKey = factory.Services.GetRequiredService<IConfiguration>()["Security:ApiKey"]!;
    }

    private HttpClient AuthorizedClient()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Api-Key", _apiKey);
        return client;
    }

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
        client.DefaultRequestHeaders.Add("X-Api-Key", "wrong-" + _apiKey);

        var response = await client.GetAsync("/api/orders");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Orders_WithCorrectKey_Returns200()
    {
        var response = await AuthorizedClient().GetAsync("/api/orders");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task CreateOrder_WithValidData_Returns201()
    {
        var response = await AuthorizedClient().PostAsJsonAsync("/api/orders", new { customer = "Acme", itemId = 1, quantity = 5 });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Theory]
    [InlineData("", 5)]
    [InlineData("   ", 5)]
    [InlineData("Acme", 0)]
    [InlineData("Acme", 1001)]
    public async Task CreateOrder_WithInvalidData_Returns400(string customer, int quantity)
    {
        var response = await AuthorizedClient().PostAsJsonAsync("/api/orders", new { customer, itemId = 1, quantity });

        Assert.Equal(400, (int)response.StatusCode);
    }

    [Fact]
    public async Task CreateOrder_WithLongCustomer_Returns400()
    {
        var response = await AuthorizedClient().PostAsJsonAsync("/api/orders", new { customer = new string('a', 81), itemId = 1, quantity = 5 });

        Assert.Equal(400, (int)response.StatusCode);
    }
}
