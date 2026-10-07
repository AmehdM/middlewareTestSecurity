using MiddlewareDemo.Api.Requests;

namespace MiddlewareDemo.Api.Validation;

public static class ProductPayloadValidator
{
    public static List<string> Validate(CreateProductPayload? payload)
    {
        var errors = new List<string>();
        if (payload is null)
        {
            errors.Add("Body is required");
            return errors;
        }

        if (!ValidationPatterns.IsSku(payload.Sku))
        {
            errors.Add("Sku must be 3-12 upper-case letters or digits");
        }

        var name = payload.Name?.Trim();
        if (string.IsNullOrEmpty(name) || name.Length > 120)
        {
            errors.Add("Name is required and must be at most 120 characters");
        }

        if (payload.Price <= 0 || payload.Price > 100000m)
        {
            errors.Add("Price must be between 0 and 100000");
        }

        if (string.IsNullOrWhiteSpace(payload.Category) || payload.Category.Length > 60)
        {
            errors.Add("Category is required and must be at most 60 characters");
        }

        if (payload.Description is { Length: > 2000 })
        {
            errors.Add("Description must be at most 2000 characters");
        }

        return errors;
    }
}
