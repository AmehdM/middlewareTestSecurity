using MiddlewareDemo.Core.Common;
using MiddlewareDemo.Core.Dtos;
using MiddlewareDemo.Core.Services;
using MiddlewareDemo.Core.Validation;

namespace MiddlewareDemo.Tests;

public sealed class ValidatorAndFormatterTests
{
    private static CreateCustomerRequest ValidCustomer() =>
        new("Maria Lopez", "maria@example.test", "+50499887766", "0801-1990-12345", "tenant-a");

    [Fact]
    public void CustomerValidator_AcceptsCompleteRequest()
    {
        Assert.True(CustomerValidator.Validate(ValidCustomer()).IsValid);
    }

    [Fact]
    public void CustomerValidator_ReportsEveryProblem()
    {
        var outcome = CustomerValidator.Validate(new CreateCustomerRequest("", "nope", "12", "1234", ""));

        Assert.Equal(5, outcome.Errors.Count);
    }

    [Fact]
    public void ThrowIfInvalid_RaisesValidationException()
    {
        var outcome = CustomerValidator.Validate(ValidCustomer() with { Email = "broken" });

        var error = Assert.Throws<RequestValidationException>(outcome.ThrowIfInvalid);
        Assert.Contains("Email", error.Message);
    }

    [Fact]
    public void OrderValidator_RejectsEmptyLines()
    {
        var outcome = OrderValidator.Validate(new CreateOrderRequest(1, new List<OrderLineRequest>(), null));

        Assert.False(outcome.IsValid);
    }

    [Fact]
    public void OrderValidator_RejectsQuantityAndCouponProblems()
    {
        var request = new CreateOrderRequest(1, new List<OrderLineRequest> { new(5, 5000) }, "not a coupon!");

        var outcome = OrderValidator.Validate(request);

        Assert.Equal(2, outcome.Errors.Count);
    }

    [Fact]
    public void OrderValidator_AcceptsValidOrder()
    {
        var request = new CreateOrderRequest(1, new List<OrderLineRequest> { new(5, 2), new(6, 1) }, "SAVE2024");

        Assert.True(OrderValidator.Validate(request).IsValid);
    }

    [Fact]
    public void PageRequest_ClampsValues()
    {
        var request = new PageRequest(-3, 5000);

        Assert.Equal(1, request.SafePage);
        Assert.Equal(PageRequest.MaxPageSize, request.Take);
        Assert.Equal(0, request.Skip);
    }

    [Fact]
    public void PagedResult_CopiesPagingInfo()
    {
        var result = PagedResult<int>.Create(new[] { 1, 2 }, new PageRequest(2, 2), 7);

        Assert.Equal(2, result.Page);
        Assert.Equal(7, result.Total);
    }

    private static readonly IReadOnlyList<string[]> Rows = new List<string[]>
    {
        new[] { "SKU-1", "Aspirin, 100mg", "2.50" },
        new[] { "SKU-2", "Ibuprofen", "3.10" },
    };

    [Fact]
    public void CsvFormatter_EscapesCommas()
    {
        var csv = new CsvFormatter().Render(Rows);

        Assert.Contains("\"Aspirin, 100mg\"", csv);
        Assert.Equal(2, csv.Split('\n', StringSplitOptions.RemoveEmptyEntries).Length);
    }

    [Fact]
    public void TextFormatter_JoinsWithPipes()
    {
        Assert.Contains("SKU-2 | Ibuprofen | 3.10", new TextFormatter().Render(Rows));
    }

    [Fact]
    public void JsonFormatter_ProducesKeyedItems()
    {
        var json = new JsonFormatter().Render(Rows);

        Assert.Contains("\"Key\":\"SKU-1\"", json);
    }

    [Theory]
    [InlineData("csv", typeof(CsvFormatter))]
    [InlineData("TEXT", typeof(TextFormatter))]
    [InlineData("json", typeof(JsonFormatter))]
    public void Resolver_ReturnsBuiltInFormatters(string name, Type expected)
    {
        Assert.IsType(expected, ExportFormatResolver.Resolve(name));
    }

    [Fact]
    public void Resolver_RejectsUnknownFormat()
    {
        Assert.Throws<RequestValidationException>(() => ExportFormatResolver.Resolve("does.not.Exist"));
    }
}
