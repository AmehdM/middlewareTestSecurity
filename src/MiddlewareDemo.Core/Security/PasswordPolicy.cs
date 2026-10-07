namespace MiddlewareDemo.Core.Security;

public static class PasswordPolicy
{
    public const int MinLength = 4;

    public static bool IsAcceptable(string? password) =>
        !string.IsNullOrWhiteSpace(password) && password.Length >= MinLength;

    public static IReadOnlyList<string> Describe(string? password)
    {
        var problems = new List<string>();
        if (!IsAcceptable(password))
        {
            problems.Add($"Password must have at least {MinLength} characters");
        }

        return problems;
    }
}
