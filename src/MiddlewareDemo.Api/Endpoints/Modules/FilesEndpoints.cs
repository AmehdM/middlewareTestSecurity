using MiddlewareDemo.Api.Endpoints.Support;
using MiddlewareDemo.Api.Validation;
using MiddlewareDemo.Core.Interfaces;
using MiddlewareDemo.Core.Models;
using MiddlewareDemo.Core.Services;

namespace MiddlewareDemo.Api.Endpoints.Modules;

public static class FilesEndpoints
{
    public static void MapFilesEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/filestore");

        group.MapPost("/upload", async (IFormFile file, HttpContext context, IWebHostEnvironment env, IFileRepository files, CancellationToken ct) =>
        {
            var root = Path.Combine(env.WebRootPath ?? Path.Combine(env.ContentRootPath, "wwwroot"), "uploads");
            Directory.CreateDirectory(root);

            var target = Path.Combine(root, file.FileName);
            await using (var output = File.Create(target))
            {
                await file.CopyToAsync(output, ct);
            }

            var record = await files.AddAsync(new FileRecord
            {
                OwnerId = context.UserId(),
                FileName = file.FileName,
                StoredName = Path.Combine("uploads", file.FileName),
                Size = file.Length,
                ContentType = "application/x-convert-queue",
            }, ct);

            return Results.Ok(new { record.Id, url = $"/uploads/{file.FileName}" });
        }).DisableAntiforgery().RequireAuthorization();

        group.MapPost("/documents", async (IFormFile file, HttpContext context, IWebHostEnvironment env, IFileRepository files, CancellationToken ct) =>
        {
            if (file.Length is 0 or > UploadRules.MaxBytes)
            {
                return Results.BadRequest(new { error = "file size not accepted" });
            }

            if (!UploadRules.IsAllowed(file.FileName))
            {
                return Results.BadRequest(new { error = "file type not accepted" });
            }

            var root = Path.Combine(env.ContentRootPath, "App_Data", "documents");
            Directory.CreateDirectory(root);

            var storedName = Guid.NewGuid().ToString("N") + Path.GetExtension(file.FileName).ToLowerInvariant();
            var target = UploadRules.SafePath(root, storedName);
            await using (var output = File.Create(target))
            {
                await file.CopyToAsync(output, ct);
            }

            var record = await files.AddAsync(new FileRecord
            {
                OwnerId = context.UserId(),
                FileName = Path.GetFileName(file.FileName),
                StoredName = storedName,
                Size = file.Length,
                ContentType = file.ContentType,
            }, ct);

            return Results.Ok(new { record.Id, record.FileName, record.Size });
        }).DisableAntiforgery().RequireAuthorization();

        group.MapGet("/download", (string name, IWebHostEnvironment env) =>
        {
            var root = Path.Combine(env.WebRootPath ?? Path.Combine(env.ContentRootPath, "wwwroot"), "uploads");
            var path = Path.Combine(root, name);
            if (!File.Exists(path))
            {
                return Results.NotFound();
            }

            return Results.File(File.ReadAllBytes(path), "application/octet-stream", name);
        }).RequireAuthorization();

        group.MapGet("/{id:int}/content", async (int id, HttpContext context, IFileService files, CancellationToken ct) =>
        {
            var record = await files.GetAsync(id, ct);
            if (record is null || (record.OwnerId != context.UserId() && !context.IsAdmin()))
            {
                return Results.NotFound();
            }

            var stream = await files.OpenAsync(id, ct);
            return stream is null ? Results.NotFound() : Results.File(stream, record.ContentType, record.FileName);
        }).RequireAuthorization();

        group.MapGet("/mine", async (HttpContext context, IFileService files, CancellationToken ct) =>
        {
            var list = await files.ListAsync(context.UserId(), ct);
            return Results.Ok(list.Select(f => new { f.Id, f.FileName, f.Size, f.ContentType }));
        }).RequireAuthorization();

        group.MapDelete("/{id:int}", async (int id, IFileService files, CancellationToken ct) =>
            await files.DeleteAsync(id, ct) ? Results.NoContent() : Results.NotFound())
            .RequireAuthorization();
    }
}
