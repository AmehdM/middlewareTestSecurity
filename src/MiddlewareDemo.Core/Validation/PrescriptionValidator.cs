using MiddlewareDemo.Core.Common;
using MiddlewareDemo.Core.Dtos;

namespace MiddlewareDemo.Core.Validation;

public static class PrescriptionValidator
{
    public static ValidationOutcome Validate(CreatePrescriptionRequest request)
    {
        var outcome = new ValidationOutcome();

        if (request.CustomerId <= 0)
        {
            outcome.Errors.Add("Customer is required");
        }

        if (string.IsNullOrWhiteSpace(request.PatientName) || request.PatientName.Length > 120)
        {
            outcome.Errors.Add("Patient name is required");
        }

        if (string.IsNullOrWhiteSpace(request.DoctorName) || request.DoctorName.Length > 120)
        {
            outcome.Errors.Add("Doctor name is required");
        }

        if (string.IsNullOrWhiteSpace(request.Diagnosis) || request.Diagnosis.Length > 2000)
        {
            outcome.Errors.Add("Diagnosis is required and limited to 2000 characters");
        }

        if (string.IsNullOrWhiteSpace(request.PatientNationalId) || request.PatientNationalId.Length is < 8 or > 20)
        {
            outcome.Errors.Add("Patient national id is not valid");
        }

        var digits = request.CardNumber?.Count(char.IsDigit) ?? 0;
        if (digits is < 13 or > 19 || !PassesLuhn(request.CardNumber!))
        {
            outcome.Errors.Add("Card number is not valid");
        }

        return outcome;
    }

    private static bool PassesLuhn(string number)
    {
        var sum = 0;
        var alternate = false;
        for (var i = number.Length - 1; i >= 0; i--)
        {
            if (!char.IsDigit(number[i]))
            {
                continue;
            }

            var d = number[i] - '0';
            if (alternate)
            {
                d *= 2;
                if (d > 9)
                {
                    d -= 9;
                }
            }

            sum += d;
            alternate = !alternate;
        }

        return sum % 10 == 0;
    }
}
