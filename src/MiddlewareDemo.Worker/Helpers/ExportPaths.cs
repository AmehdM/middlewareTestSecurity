namespace MiddlewareDemo.Worker.Helpers;

public static class ExportPaths
{
    public static string ForJob(int jobId, string extension) =>
        Path.Combine(Path.GetTempPath(), $"export_{jobId}.{extension}");

    public static string Scratch(string prefix) =>
        Path.Combine(Path.GetTempPath(), $"{prefix}_{new Random().Next(1000, 9999)}.tmp");
}
