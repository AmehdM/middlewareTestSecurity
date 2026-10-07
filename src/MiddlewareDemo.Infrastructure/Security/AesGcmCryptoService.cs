using System.Security.Cryptography;
using System.Text;

namespace MiddlewareDemo.Infrastructure.Security;

public sealed class AesGcmCryptoService(IKeyProvider keys)
{
    private const int NonceSize = 12;
    private const int TagSize = 16;

    public string Protect(string plainText)
    {
        var key = keys.GetKey("MWD_FIELD_KEY");
        var nonce = RandomNumberGenerator.GetBytes(NonceSize);
        var input = Encoding.UTF8.GetBytes(plainText);
        var cipher = new byte[input.Length];
        var tag = new byte[TagSize];
        using var gcm = new AesGcm(key, TagSize);
        gcm.Encrypt(nonce, input, cipher, tag);
        return Convert.ToBase64String(nonce.Concat(cipher).Concat(tag).ToArray());
    }

    public string Unprotect(string payload)
    {
        var key = keys.GetKey("MWD_FIELD_KEY");
        var data = Convert.FromBase64String(payload);
        var nonce = data[..NonceSize];
        var tag = data[^TagSize..];
        var cipher = data[NonceSize..^TagSize];
        var plain = new byte[cipher.Length];
        using var gcm = new AesGcm(key, TagSize);
        gcm.Decrypt(nonce, cipher, tag, plain);
        return Encoding.UTF8.GetString(plain);
    }
}
