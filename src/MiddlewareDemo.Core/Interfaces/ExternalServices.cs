using MiddlewareDemo.Core.Dtos;
using MiddlewareDemo.Core.Models;

namespace MiddlewareDemo.Core.Interfaces;

public interface ICryptoService
{
    string Encrypt(string plainText);
    string Decrypt(string cipherText);
}

public interface IFileStorage
{
    Task<string> SaveAsync(string storedName, Stream content, CancellationToken ct = default);
    Task<Stream?> OpenReadAsync(string storedName, CancellationToken ct = default);
    Task DeleteAsync(string storedName, CancellationToken ct = default);
}

public interface IRatesClient
{
    Task<decimal> GetRateAsync(string currency, CancellationToken ct = default);
}

public interface IMailSender
{
    Task SendAsync(string to, string subject, string htmlBody, CancellationToken ct = default);
}

public interface ISmsSender
{
    Task SendAsync(string phone, string text, CancellationToken ct = default);
}

public interface ITokenIssuer
{
    Task<TokenPair> IssueAsync(User user, CancellationToken ct = default);
    Task<TokenPair?> RefreshAsync(string refreshToken, CancellationToken ct = default);
    Task RevokeAsync(string refreshToken, CancellationToken ct = default);
}

public interface IApiKeyStore
{
    Task<string?> GetSecretHashAsync(string clientId, CancellationToken ct = default);
    Task SaveSecretHashAsync(string clientId, string secretHash, CancellationToken ct = default);
    Task<bool> ValidateAsync(string apiKey, CancellationToken ct = default);
}

public interface ICacheStore
{
    Task<T?> GetAsync<T>(string key, CancellationToken ct = default);
    Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken ct = default);
    Task RemoveAsync(string key, CancellationToken ct = default);
}
