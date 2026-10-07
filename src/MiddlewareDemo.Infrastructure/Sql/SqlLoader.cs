using System.Reflection;

namespace MiddlewareDemo.Infrastructure.Sql;

public static class SqlLoader
{
    public static string Load(string name)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resource = $"MiddlewareDemo.Infrastructure.Sql.{name}.sql";
        using var stream = assembly.GetManifestResourceStream(resource)
            ?? throw new FileNotFoundException(resource);
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
