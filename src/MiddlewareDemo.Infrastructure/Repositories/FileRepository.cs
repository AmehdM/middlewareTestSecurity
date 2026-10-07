using MiddlewareDemo.Core.Interfaces;
using MiddlewareDemo.Core.Models;
using MiddlewareDemo.Infrastructure.Data;

namespace MiddlewareDemo.Infrastructure.Repositories;

public sealed class FileRepository(SqliteDb db) : IFileRepository
{
    private const string Columns = "id,owner_id,file_name,stored_name,size,content_type";

    public async Task<FileRecord> AddAsync(FileRecord record, CancellationToken ct = default)
    {
        await using var connection = await db.OpenAsync(ct);
        record.Id = await connection.InsertAsync("INSERT INTO files (owner_id,file_name,stored_name,size,content_type) VALUES (@o,@f,@s,@z,@c)", ct,
            ("@o", record.OwnerId), ("@f", record.FileName), ("@s", record.StoredName), ("@z", record.Size), ("@c", record.ContentType));
        return record;
    }

    public async Task<FileRecord?> GetAsync(int id, CancellationToken ct = default)
    {
        await using var connection = await db.OpenAsync(ct);
        await using var command = connection.Command($"SELECT {Columns} FROM files WHERE id=@id", ("@id", id));
        await using var reader = await command.ExecuteReaderAsync(ct);
        return await reader.ReadAsync(ct) ? Map(reader) : null;
    }

    public async Task<IReadOnlyList<FileRecord>> ListByOwnerAsync(int ownerId, CancellationToken ct = default)
    {
        await using var connection = await db.OpenAsync(ct);
        await using var command = connection.Command($"SELECT {Columns} FROM files WHERE owner_id=@o ORDER BY id DESC", ("@o", ownerId));
        await using var reader = await command.ExecuteReaderAsync(ct);
        var result = new List<FileRecord>();
        while (await reader.ReadAsync(ct))
        {
            result.Add(Map(reader));
        }

        return result;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken ct = default)
    {
        await using var connection = await db.OpenAsync(ct);
        await using var command = connection.Command("DELETE FROM files WHERE id=@id", ("@id", id));
        return await command.ExecuteNonQueryAsync(ct) > 0;
    }

    private static FileRecord Map(Microsoft.Data.Sqlite.SqliteDataReader r) => new()
    {
        Id = r.Int("id"),
        OwnerId = r.Int("owner_id"),
        FileName = r.Str("file_name"),
        StoredName = r.Str("stored_name"),
        Size = r.GetInt64(r.GetOrdinal("size")),
        ContentType = r.Str("content_type")
    };
}
