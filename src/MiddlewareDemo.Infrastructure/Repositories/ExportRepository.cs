using MiddlewareDemo.Core.Interfaces;
using MiddlewareDemo.Core.Models;
using MiddlewareDemo.Infrastructure.Data;

namespace MiddlewareDemo.Infrastructure.Repositories;

public sealed class ExportRepository(SqliteDb db) : IExportRepository
{
    public async Task<ExportJob> AddAsync(ExportJob job, CancellationToken ct = default)
    {
        await using var connection = await db.OpenAsync(ct);
        job.Id = await connection.InsertAsync("INSERT INTO exports (dataset,format,status,requested_by,result_path,created_at) VALUES (@d,@f,@s,@r,@p,@c)", ct,
            ("@d", job.Dataset), ("@f", job.Format), ("@s", job.Status), ("@r", job.RequestedBy), ("@p", job.ResultPath), ("@c", job.CreatedAt.Iso()));
        return job;
    }

    public async Task<ExportJob?> GetAsync(int id, CancellationToken ct = default)
    {
        await using var connection = await db.OpenAsync(ct);
        await using var command = connection.Command("SELECT id,dataset,format,status,requested_by,result_path,created_at FROM exports WHERE id=@id", ("@id", id));
        await using var reader = await command.ExecuteReaderAsync(ct);
        if (!await reader.ReadAsync(ct))
        {
            return null;
        }

        return new ExportJob
        {
            Id = reader.Int("id"),
            Dataset = reader.Str("dataset"),
            Format = reader.Str("format"),
            Status = reader.Str("status"),
            RequestedBy = reader.Str("requested_by"),
            ResultPath = reader.Str("result_path"),
            CreatedAt = reader.Date("created_at")
        };
    }

    public async Task UpdateAsync(ExportJob job, CancellationToken ct = default)
    {
        await using var connection = await db.OpenAsync(ct);
        await using var command = connection.Command("UPDATE exports SET status=@s,result_path=@p WHERE id=@id",
            ("@s", job.Status), ("@p", job.ResultPath), ("@id", job.Id));
        await command.ExecuteNonQueryAsync(ct);
    }
}
