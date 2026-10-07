using System.IO.Compression;

namespace MiddlewareDemo.Infrastructure.Storage;

public sealed class SafeArchiveExtractor
{
    private const int MaxEntries = 200;
    private const long MaxTotalBytes = 50 * 1024 * 1024;

    public IReadOnlyList<string> Extract(Stream archive, string destination)
    {
        var root = Path.GetFullPath(destination).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        Directory.CreateDirectory(root);
        var written = new List<string>();
        long total = 0;
        using var zip = new ZipArchive(archive, ZipArchiveMode.Read);
        if (zip.Entries.Count > MaxEntries)
        {
            throw new InvalidDataException("Archive has too many entries.");
        }

        foreach (var entry in zip.Entries)
        {
            var target = Path.GetFullPath(Path.Combine(root, entry.FullName));
            if (!target.StartsWith(root, StringComparison.Ordinal))
            {
                throw new InvalidDataException("Archive entry escapes the destination.");
            }

            if (entry.Name.Length == 0)
            {
                continue;
            }

            total += entry.Length;
            if (total > MaxTotalBytes)
            {
                throw new InvalidDataException("Archive is too large.");
            }

            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            entry.ExtractToFile(target, false);
            written.Add(target);
        }

        return written;
    }
}
