using System.Text.RegularExpressions;

namespace MiddlewareDemo.Api.Validation;

public static class ValidationPatterns
{
    private static readonly TimeSpan Timeout = TimeSpan.FromMilliseconds(100);

    private static readonly Regex Sku = new(
        @"^[A-Z0-9]{3,12}(-[A-Z0-9]{1,6}){0,2}$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant,
        Timeout);

    private static readonly Regex Currency = new(
        @"^[A-Z]{3}$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant,
        Timeout);

    public static bool IsSku(string? value) => value is not null && Sku.IsMatch(value);

    public static bool IsCurrency(string? value) => value is not null && Currency.IsMatch(value);
}
