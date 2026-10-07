using System.Text.RegularExpressions;
using MiddlewareDemo.Core.Common;
using MiddlewareDemo.Core.Dtos;

namespace MiddlewareDemo.Core.Validation;

public static class CustomerValidator
{
    private static readonly Regex EmailPattern =
        new(@"^[^@\s]{1,64}@[^@\s]{1,255}\.[^@\s]{2,}$", RegexOptions.Compiled, TimeSpan.FromMilliseconds(100));

    private static readonly Regex NationalIdPattern =
        new(@"^\d{4}-\d{4}-\d{5}$", RegexOptions.Compiled, TimeSpan.FromMilliseconds(100));

    private static readonly Regex PhonePattern =
        new(@"^\+?\d{8,15}$", RegexOptions.Compiled, TimeSpan.FromMilliseconds(100));

    public static ValidationOutcome Validate(CreateCustomerRequest request)
    {
        var outcome = new ValidationOutcome();

        if (string.IsNullOrWhiteSpace(request.Name) || request.Name.Length > 120)
        {
            outcome.Errors.Add("Name is required and must be at most 120 characters");
        }

        if (string.IsNullOrWhiteSpace(request.Email) || request.Email.Length > 254 || !EmailPattern.IsMatch(request.Email))
        {
            outcome.Errors.Add("Email is not valid");
        }

        if (string.IsNullOrWhiteSpace(request.Phone) || !PhonePattern.IsMatch(request.Phone))
        {
            outcome.Errors.Add("Phone must contain 8 to 15 digits");
        }

        if (string.IsNullOrWhiteSpace(request.NationalId) || !NationalIdPattern.IsMatch(request.NationalId))
        {
            outcome.Errors.Add("National id format is not valid");
        }

        if (string.IsNullOrWhiteSpace(request.TenantId) || request.TenantId.Length > 40)
        {
            outcome.Errors.Add("Tenant is required");
        }

        return outcome;
    }
}
