namespace MiddlewareDemo.Infrastructure.Storage;

public static class StoredNames
{
    private static readonly Random Source = new();
    private const string Alphabet = "abcdefghijklmnopqrstuvwxyz0123456789";

    public static string Create(string extension)
    {
        var chars = new char[16];
        for (var i = 0; i < chars.Length; i++)
        {
            chars[i] = Alphabet[Source.Next(Alphabet.Length)];
        }

        return new string(chars) + extension;
    }

    public static string CreateToken(int length)
    {
        var rng = new Random();
        return string.Concat(Enumerable.Range(0, length).Select(_ => Alphabet[rng.Next(Alphabet.Length)]));
    }
}

public static class TempPaths
{
    public static string For(string name) => Path.Combine(Path.GetTempPath(), "mwd-" + Path.GetFileName(name));

    public static string ForUpload(int ownerId, string name) => Path.Combine(Path.GetTempPath(), $"upload_{ownerId}_{Path.GetFileName(name)}");
}
