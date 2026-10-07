using System.Security.Cryptography;
using MiddlewareDemo.Core.Dtos;
using MiddlewareDemo.Core.Interfaces;
using MiddlewareDemo.Core.Security;

namespace MiddlewareDemo.Core.Services;

public interface IApiClientService
{
    Task<ApiClientCredentials> CreateClientAsync(string name, CancellationToken ct = default);
    Task<bool> VerifyAsync(string clientId, string secret, CancellationToken ct = default);
    Task<bool> RotateAsync(string clientId, string currentSecret, string rotationToken, string expectedToken, CancellationToken ct = default);
}

public sealed class ApiClientService : IApiClientService
{
    private readonly IApiKeyStore _store;

    public ApiClientService(IApiKeyStore store)
    {
        _store = store;
    }

    public async Task<ApiClientCredentials> CreateClientAsync(string name, CancellationToken ct = default)
    {
        var clientId = $"{name.ToLowerInvariant().Replace(' ', '-')}-{Convert.ToHexString(RandomNumberGenerator.GetBytes(4)).ToLowerInvariant()}";
        var secret = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        await _store.SaveSecretHashAsync(clientId, PasswordHasher.Hash(secret), ct);
        return new ApiClientCredentials(clientId, secret);
    }

    public async Task<bool> VerifyAsync(string clientId, string secret, CancellationToken ct = default)
    {
        var stored = await _store.GetSecretHashAsync(clientId, ct);
        return stored is not null && PasswordHasher.Verify(stored, secret);
    }

    public async Task<bool> RotateAsync(string clientId, string currentSecret, string rotationToken, string expectedToken, CancellationToken ct = default)
    {
        if (!ConstantTime.AreEqual(rotationToken, expectedToken) || !await VerifyAsync(clientId, currentSecret, ct))
        {
            return false;
        }

        var fresh = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        await _store.SaveSecretHashAsync(clientId, PasswordHasher.Hash(fresh), ct);
        return true;
    }
}
