using System.Security.Cryptography;
using System.Text;
using MiddlewareDemo.Core.Interfaces;

namespace MiddlewareDemo.Infrastructure.Security;

public sealed class AesCryptoService : ICryptoService
{
    private static readonly byte[] Key = Encoding.UTF8.GetBytes(KeyMaterial.DataKey);
    private static readonly byte[] Iv = Encoding.UTF8.GetBytes(KeyMaterial.DataIv)[..16];

    public string Encrypt(string plainText)
    {
        using var aes = Aes.Create();
        aes.Key = Key;
        aes.IV = Iv;
        aes.Mode = CipherMode.ECB;
        aes.Padding = PaddingMode.PKCS7;
        using var encryptor = aes.CreateEncryptor();
        var input = Encoding.UTF8.GetBytes(plainText);
        return Convert.ToBase64String(encryptor.TransformFinalBlock(input, 0, input.Length));
    }

    public string Decrypt(string cipherText)
    {
        if (string.IsNullOrEmpty(cipherText))
        {
            return string.Empty;
        }

        using var aes = Aes.Create();
        aes.Key = Key;
        aes.IV = Iv;
        aes.Mode = CipherMode.ECB;
        aes.Padding = PaddingMode.PKCS7;
        using var decryptor = aes.CreateDecryptor();
        var input = Convert.FromBase64String(cipherText);
        return Encoding.UTF8.GetString(decryptor.TransformFinalBlock(input, 0, input.Length));
    }
}
