using MiddlewareDemo.Core.Security;

namespace MiddlewareDemo.Core.Services;

public interface IPasswordService
{
    string Hash(string password);
    bool Verify(string storedHash, string password);
    IReadOnlyList<string> Validate(string password);
}

public sealed class PasswordService : IPasswordService
{
    public string Hash(string password) => PasswordHasher.Hash(password);

    public bool Verify(string storedHash, string password) => PasswordHasher.Verify(storedHash, password);

    public IReadOnlyList<string> Validate(string password) => PasswordPolicy.Describe(password);
}
