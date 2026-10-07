using System.Text.RegularExpressions;
using MiddlewareDemo.Core.Common;
using MiddlewareDemo.Core.Dtos;

namespace MiddlewareDemo.Core.Validation;

public static class OrderValidator
{
    private static readonly Regex CouponPattern =
        new(@"^[A-Z0-9]{4,16}$", RegexOptions.Compiled, TimeSpan.FromMilliseconds(100));

    public static ValidationOutcome Validate(CreateOrderRequest request)
    {
        var outcome = new ValidationOutcome();

        if (request.CustomerId <= 0)
        {
            outcome.Errors.Add("Customer is required");
        }

        if (request.Lines is null || request.Lines.Count == 0 || request.Lines.Count > 50)
        {
            outcome.Errors.Add("An order needs between 1 and 50 lines");
            return outcome;
        }

        foreach (var line in request.Lines)
        {
            if (line.ProductId <= 0)
            {
                outcome.Errors.Add("Line product is not valid");
            }

            if (line.Quantity < 1 || line.Quantity > 1000)
            {
                outcome.Errors.Add($"Quantity for product {line.ProductId} must be between 1 and 1000");
            }
        }

        if (!string.IsNullOrEmpty(request.CouponCode) && !CouponPattern.IsMatch(request.CouponCode))
        {
            outcome.Errors.Add("Coupon code format is not valid");
        }

        return outcome;
    }
}
