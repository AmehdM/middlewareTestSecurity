namespace MiddlewareDemo.Core.Logging;

public static class MaskingHelper
{
    public static string MaskCard(string? pan)
    {
        var digits = new string((pan ?? string.Empty).Where(char.IsDigit).ToArray());
        if (digits.Length < 8)
        {
            return "****";
        }

        return new string('*', digits.Length - 4) + digits[^4..];
    }

    public static string MaskNationalId(string? nationalId)
    {
        if (string.IsNullOrEmpty(nationalId) || nationalId.Length <= 4)
        {
            return "****";
        }

        return new string('*', nationalId.Length - 4) + nationalId[^4..];
    }
}
