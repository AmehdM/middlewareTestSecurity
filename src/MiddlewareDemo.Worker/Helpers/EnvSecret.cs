namespace MiddlewareDemo.Worker.Helpers;

public static class EnvSecret
{
    public static string Require(string name)
    {
        var value = Environment.GetEnvironmentVariable(name);
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"Required environment variable {name} is not set");
        }

        return value;
    }
}
