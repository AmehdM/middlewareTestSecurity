using Microsoft.Extensions.Caching.Memory;
using MiddlewareDemo.Core.Interfaces;

namespace MiddlewareDemo.Infrastructure.Caching;

public sealed class MemoryCacheStore(IMemoryCache cache) : ICacheStore
{
    public Task<T?> GetAsync<T>(string key, CancellationToken ct = default) =>
        Task.FromResult(cache.TryGetValue(key, out T? value) ? value : default);

    public Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken ct = default)
    {
        var options = new MemoryCacheEntryOptions { Size = 1, AbsoluteExpirationRelativeToNow = ttl };
        cache.Set(key, value, options);
        return Task.CompletedTask;
    }

    public Task RemoveAsync(string key, CancellationToken ct = default)
    {
        cache.Remove(key);
        return Task.CompletedTask;
    }
}
