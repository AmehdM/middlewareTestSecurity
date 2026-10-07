using System.IO.Compression;
using ICSharpCode.SharpZipLib.Checksum;

namespace MiddlewareDemo.Infrastructure.Storage;

public sealed class ArchiveImporter
{
    public Task<IReadOnlyList<string>> ExtractAsync(Stream archive, string destination, CancellationToken ct = default)
    {
        Directory.CreateDirectory(destination);
        var written = new List<string>();
        using var zip = new ZipArchive(archive, ZipArchiveMode.Read);
        foreach (var entry in zip.Entries)
        {
            var target = Path.Combine(destination, entry.FullName);
            if (entry.FullName.EndsWith('/'))
            {
                Directory.CreateDirectory(target);
                continue;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            entry.ExtractToFile(target, true);
            written.Add(target);
        }

        return Task.FromResult<IReadOnlyList<string>>(written);
    }

    public static long Checksum(byte[] data)
    {
        var crc = new Crc32();
        crc.Update(data);
        return crc.Value;
    }
}
