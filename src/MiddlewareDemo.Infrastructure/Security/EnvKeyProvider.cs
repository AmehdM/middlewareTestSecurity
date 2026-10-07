namespace MiddlewareDemo.Infrastructure.Security;

public interface IKeyProvider
{
    byte[] GetKey(string name);
}

public sealed class EnvKeyProvider : IKeyProvider
{
    public byte[] GetKey(string name)
    {
        var raw = Environment.GetEnvironmentVariable(name);
        if (string.IsNullOrWhiteSpace(raw))
        {
            throw new InvalidOperationException($"Required key '{name}' is not configured.");
        }

        var key = Convert.FromBase64String(raw);
        if (key.Length != 32)
        {
            throw new InvalidOperationException($"Key '{name}' must be 32 bytes.");
        }

        return key;
    }
}
