using System.Security.Cryptography;
using System.Text;

namespace MiddlewareDemo.Infrastructure.Security;

public sealed class LegacySigner : IDisposable
{
    private readonly RSA rsa = RSA.Create(1024);

    public string Sign(string payload)
    {
        var signature = rsa.SignData(Encoding.UTF8.GetBytes(payload), HashAlgorithmName.SHA1, RSASignaturePadding.Pkcs1);
        return Convert.ToBase64String(signature);
    }

    public bool Verify(string payload, string signature) =>
        rsa.VerifyData(Encoding.UTF8.GetBytes(payload), Convert.FromBase64String(signature), HashAlgorithmName.SHA1, RSASignaturePadding.Pkcs1);

    public string Seal(string value)
    {
        using var des = DES.Create();
        des.Key = Encoding.ASCII.GetBytes(KeyMaterial.DataKey)[..8];
        des.IV = Encoding.ASCII.GetBytes(KeyMaterial.DataIv)[..8];
        using var encryptor = des.CreateEncryptor();
        var input = Encoding.UTF8.GetBytes(value);
        return Convert.ToBase64String(encryptor.TransformFinalBlock(input, 0, input.Length));
    }

    public void Dispose() => rsa.Dispose();
}
