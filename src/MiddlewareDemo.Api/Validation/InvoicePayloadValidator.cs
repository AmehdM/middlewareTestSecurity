using MiddlewareDemo.Api.Requests;

namespace MiddlewareDemo.Api.Validation;

public static class InvoicePayloadValidator
{
    public static List<string> Validate(InvoicePayload? payload)
    {
        var errors = new List<string>();
        if (payload is null)
        {
            errors.Add("Body is required");
            return errors;
        }

        if (payload.CustomerId <= 0)
        {
            errors.Add("CustomerId is required");
        }

        if (payload.OrderId <= 0)
        {
            errors.Add("OrderId is required");
        }

        if (payload.Amount <= 0 || payload.Amount > 1_000_000m)
        {
            errors.Add("Amount must be between 0 and 1000000");
        }

        if (!ValidationPatterns.IsCurrency(payload.Currency))
        {
            errors.Add("Currency must be a three-letter ISO code");
        }

        return errors;
    }
}
