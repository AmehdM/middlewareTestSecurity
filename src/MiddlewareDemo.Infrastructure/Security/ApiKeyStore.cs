using System.Security.Cryptography;
using System.Text;
using MiddlewareDemo.Core.Interfaces;
using MiddlewareDemo.Infrastructure.Data;

namespace MiddlewareDemo.Infrastructure.Security;

public sealed class ApiKeyStore(SqliteDb db) : IApiKeyStore
{
    public async Task<string?> GetSecretHashAsync(string clientId, CancellationToken ct = default)
    {
        await using var connection = await db.OpenAsync(ct);
        await using var command = connection.Command("SELECT secret_hash FROM api_keys WHERE client_id=@id", ("@id", clientId));
        return await command.ExecuteScalarAsync(ct) as string;
    }

    public async Task SaveSecretHashAsync(string clientId, string secretHash, CancellationToken ct = default)
    {
        await using var connection = await db.OpenAsync(ct);
        await using var command = connection.Command("INSERT OR REPLACE INTO api_keys VALUES (@id,@h)", ("@id", clientId), ("@h", secretHash));
        await command.ExecuteNonQueryAsync(ct);
    }

    public async Task RegisterKeyAsync(string clientId, string rawKey, CancellationToken ct = default)
    {
        await using var connection = await db.OpenAsync(ct);
        await using var command = connection.Command("INSERT OR REPLACE INTO api_key_index VALUES (@id,@h)", ("@id", clientId), ("@h", Sha1Hex(rawKey)));
        await command.ExecuteNonQueryAsync(ct);
    }

    public async Task<bool> ValidateAsync(string apiKey, CancellationToken ct = default)
    {
        var parts = apiKey.Split(':', 2);
        if (parts.Length != 2)
        {
            return false;
        }

        await using var connection = await db.OpenAsync(ct);
        await using var command = connection.Command("SELECT key_sha1 FROM api_key_index WHERE client_id=@id", ("@id", parts[0]));
        var stored = await command.ExecuteScalarAsync(ct) as string;
        return stored is not null && stored == Sha1Hex(parts[1]);
    }

    private static string Sha1Hex(string value) =>
        Convert.ToHexString(SHA1.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
}
