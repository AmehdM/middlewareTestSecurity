using MiddlewareDemo.Core.Common;
using MiddlewareDemo.Core.Dtos;
using MiddlewareDemo.Core.Interfaces;
using MiddlewareDemo.Core.Models;

namespace MiddlewareDemo.Core.Services;

public interface IFileService
{
    Task<StoredFileDto> StoreAsync(int ownerId, string fileName, Stream content, string contentType, CancellationToken ct = default);
    Task<FileRecord?> GetAsync(int id, CancellationToken ct = default);
    Task<Stream?> OpenAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<FileRecord>> ListAsync(int ownerId, CancellationToken ct = default);
    Task<bool> DeleteAsync(int id, CancellationToken ct = default);
    Task<string> CreateTempCopyAsync(string fileName, Stream content, CancellationToken ct = default);
}

public sealed class FileService : IFileService
{
    private const long MaxBytes = 5 * 1024 * 1024;

    private static readonly HashSet<string> AllowedExtensions =
        new(StringComparer.OrdinalIgnoreCase) { ".pdf", ".png", ".jpg", ".jpeg", ".txt" };

    private readonly IFileRepository _files;
    private readonly IFileStorage _storage;

    public FileService(IFileRepository files, IFileStorage storage)
    {
        _files = files;
        _storage = storage;
    }

    public async Task<StoredFileDto> StoreAsync(int ownerId, string fileName, Stream content, string contentType, CancellationToken ct = default)
    {
        var extension = Path.GetExtension(fileName);
        if (string.IsNullOrEmpty(extension) || !AllowedExtensions.Contains(extension))
        {
            throw new RequestValidationException("File type is not allowed");
        }

        using var buffer = new MemoryStream();
        var chunk = new byte[8192];
        int read;
        while ((read = await content.ReadAsync(chunk, ct)) > 0)
        {
            if (buffer.Length + read > MaxBytes)
            {
                throw new RequestValidationException("File exceeds the 5 MB limit");
            }

            buffer.Write(chunk, 0, read);
        }

        buffer.Position = 0;
        var storedName = Guid.NewGuid().ToString("N") + extension.ToLowerInvariant();
        await _storage.SaveAsync(storedName, buffer, ct);

        var record = await _files.AddAsync(new FileRecord
        {
            OwnerId = ownerId,
            FileName = Path.GetFileName(fileName),
            StoredName = storedName,
            Size = buffer.Length,
            ContentType = contentType,
        }, ct);

        return new StoredFileDto(record.Id, record.FileName, record.Size, record.ContentType);
    }

    public Task<FileRecord?> GetAsync(int id, CancellationToken ct = default) => _files.GetAsync(id, ct);

    public async Task<Stream?> OpenAsync(int id, CancellationToken ct = default)
    {
        var record = await _files.GetAsync(id, ct);
        return record is null ? null : await _storage.OpenReadAsync(record.StoredName, ct);
    }

    public Task<IReadOnlyList<FileRecord>> ListAsync(int ownerId, CancellationToken ct = default) =>
        _files.ListByOwnerAsync(ownerId, ct);

    public async Task<bool> DeleteAsync(int id, CancellationToken ct = default)
    {
        var record = await _files.GetAsync(id, ct);
        if (record is null)
        {
            return false;
        }

        await _storage.DeleteAsync(record.StoredName, ct);
        return await _files.DeleteAsync(id, ct);
    }

    public async Task<string> CreateTempCopyAsync(string fileName, Stream content, CancellationToken ct = default)
    {
        var path = Path.Combine(Path.GetTempPath(), Path.GetFileName(fileName) + ".tmp");
        await using var target = File.Create(path);
        await content.CopyToAsync(target, ct);
        return path;
    }
}
