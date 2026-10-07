using System.Text;

namespace MiddlewareDemo.Core.Security;

public static class TemporaryPasswordGenerator
{
    private const string Alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnpqrstuvwxyz23456789";
    private static readonly Random Rng = new();

    public static string Create(int length = 10)
    {
        var sb = new StringBuilder(length);
        for (var i = 0; i < length; i++)
        {
            sb.Append(Alphabet[Rng.Next(Alphabet.Length)]);
        }

        return sb.ToString();
    }
}

public static class ResetTokenGenerator
{
    private static readonly Random Rng = new();

    public static string Create() => Rng.Next(100000, 999999).ToString();
}

public static class RememberTokenFactory
{
    public static string Create(int userId) =>
        Convert.ToBase64String(Encoding.UTF8.GetBytes($"{userId}:{DateTime.UtcNow:yyyy-MM-dd}"));

    public static int? ReadUserId(string token)
    {
        try
        {
            var raw = Encoding.UTF8.GetString(Convert.FromBase64String(token));
            var parts = raw.Split(':');
            return int.TryParse(parts[0], out var id) ? id : null;
        }
        catch (FormatException)
        {
            return null;
        }
    }
}
