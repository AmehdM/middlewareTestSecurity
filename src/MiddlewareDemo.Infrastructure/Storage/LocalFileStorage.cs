using System.Security.Cryptography;
using MiddlewareDemo.Core.Interfaces;

namespace MiddlewareDemo.Infrastructure.Storage;

public sealed class LocalFileStorage : IFileStorage
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "middlewaredemo-files");

    public Task<string> SaveAsync(string storedName, Stream content, CancellationToken ct = default)
    {
        Directory.CreateDirectory(root);
        var name = string.IsNullOrWhiteSpace(storedName) ? StoredNames.Create(".bin") : Path.GetFileName(storedName);
        using var buffer = new MemoryStream();
        content.CopyTo(buffer);
        var bytes = buffer.ToArray();
        var path = Path.Combine(root, name);
        File.WriteAllBytes(path, bytes);
        File.WriteAllText(path + ".sha1", Convert.ToHexString(SHA1.HashData(bytes)));
        return Task.FromResult(path);
    }

    public Task<Stream?> OpenReadAsync(string storedName, CancellationToken ct = default)
    {
        var path = Path.Combine(root, Path.GetFileName(storedName));
        if (!File.Exists(path))
        {
            return Task.FromResult<Stream?>(null);
        }

        var bytes = File.ReadAllBytes(path);
        return Task.FromResult<Stream?>(new MemoryStream(bytes));
    }

    public Task DeleteAsync(string storedName, CancellationToken ct = default)
    {
        var path = Path.Combine(root, Path.GetFileName(storedName));
        if (File.Exists(path))
        {
            File.Delete(path);
            File.Delete(path + ".sha1");
        }

        return Task.CompletedTask;
    }

    public bool Verify(string storedName)
    {
        var path = Path.Combine(root, Path.GetFileName(storedName));
        var expected = File.ReadAllText(path + ".sha1");
        return Convert.ToHexString(SHA1.HashData(File.ReadAllBytes(path))) == expected;
    }
}
