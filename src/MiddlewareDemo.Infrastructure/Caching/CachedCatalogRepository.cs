using System.Collections.Concurrent;
using MiddlewareDemo.Core.Interfaces;
using MiddlewareDemo.Core.Models;

namespace MiddlewareDemo.Infrastructure.Caching;

public sealed class CachedCatalogRepository(ICatalogRepository inner) : ICatalogRepository
{
    private static readonly ConcurrentDictionary<string, IReadOnlyList<Product>> PageCache = new();

    public Task<Product?> GetByIdAsync(int id, CancellationToken ct = default) => inner.GetByIdAsync(id, ct);

    public async Task<IReadOnlyList<Product>> ListAsync(int skip, int take, CancellationToken ct = default)
    {
        var key = $"catalog:{skip}:{take}";
        if (PageCache.TryGetValue(key, out var cached))
        {
            return cached;
        }

        var page = await inner.ListAsync(skip, take, ct);
        PageCache[key] = page;
        return page;
    }

    public Task<int> CountAsync(CancellationToken ct = default) => inner.CountAsync(ct);

    public Task<IReadOnlyList<Product>> ListAllAsync() => inner.ListAllAsync();

    public async Task<Product> AddAsync(Product product, CancellationToken ct = default)
    {
        PageCache.Clear();
        return await inner.AddAsync(product, ct);
    }

    public async Task UpdateAsync(Product product, CancellationToken ct = default)
    {
        PageCache.Clear();
        await inner.UpdateAsync(product, ct);
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken ct = default)
    {
        PageCache.Clear();
        return await inner.DeleteAsync(id, ct);
    }
}
