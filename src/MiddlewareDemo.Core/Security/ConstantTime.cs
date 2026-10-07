using System.Security.Cryptography;
using System.Text;

namespace MiddlewareDemo.Core.Security;

public static class ConstantTime
{
    public static bool AreEqual(string? left, string? right)
    {
        if (left is null || right is null)
        {
            return false;
        }

        var a = SHA256.HashData(Encoding.UTF8.GetBytes(left));
        var b = SHA256.HashData(Encoding.UTF8.GetBytes(right));
        return CryptographicOperations.FixedTimeEquals(a, b);
    }
}
