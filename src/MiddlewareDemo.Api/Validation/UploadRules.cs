namespace MiddlewareDemo.Api.Validation;

public static class UploadRules
{
    public const long MaxBytes = 2 * 1024 * 1024;

    private static readonly HashSet<string> Allowed =
        new(StringComparer.OrdinalIgnoreCase) { ".pdf", ".png", ".jpg", ".jpeg" };

    public static bool IsAllowed(string? fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return false;
        }

        var extension = Path.GetExtension(fileName);
        return !string.IsNullOrEmpty(extension) && Allowed.Contains(extension);
    }

    public static string SafePath(string root, string storedName)
    {
        var full = Path.GetFullPath(Path.Combine(root, storedName));
        var prefix = Path.GetFullPath(root) + Path.DirectorySeparatorChar;
        if (!full.StartsWith(prefix, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Invalid path");
        }

        return full;
    }
}
