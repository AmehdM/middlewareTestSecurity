using System.Text;
using System.Text.Json;
using MiddlewareDemo.Core.Common;
using MiddlewareDemo.Core.Dtos;

namespace MiddlewareDemo.Core.Services;

public interface IExportFormatter
{
    string Render(IReadOnlyList<string[]> rows);
}

public sealed class CsvFormatter : IExportFormatter
{
    public string Render(IReadOnlyList<string[]> rows)
    {
        var output = string.Empty;
        foreach (var row in rows)
        {
            output += string.Join(",", row.Select(Escape)) + "\n";
        }

        return output;
    }

    private static string Escape(string value) =>
        value.Contains(',') || value.Contains('"') ? "\"" + value.Replace("\"", "\"\"") + "\"" : value;
}

public sealed class TextFormatter : IExportFormatter
{
    public string Render(IReadOnlyList<string[]> rows)
    {
        var sb = new StringBuilder();
        foreach (var row in rows)
        {
            sb.AppendLine(string.Join(" | ", row));
        }

        return sb.ToString();
    }
}

public sealed class JsonFormatter : IExportFormatter
{
    public string Render(IReadOnlyList<string[]> rows)
    {
        var items = rows.Select(r => new ExportRow(r.FirstOrDefault() ?? string.Empty, string.Join(";", r.Skip(1)))).ToList();
        return JsonSerializer.Serialize(items);
    }
}

public static class ExportFormatResolver
{
    public static IExportFormatter Resolve(string format)
    {
        switch (format.ToLowerInvariant())
        {
            case "csv":
                return new CsvFormatter();
            case "text":
                return new TextFormatter();
            case "json":
                return new JsonFormatter();
        }

        var type = Type.GetType(format, throwOnError: false)
            ?? throw new RequestValidationException("Unknown export format");
        return (IExportFormatter)Activator.CreateInstance(type)!;
    }
}
