using MiddlewareDemo.Core.Interfaces;
using MiddlewareDemo.Core.Models;

namespace MiddlewareDemo.Infrastructure.Caching;

public sealed class CachedCustomerRepository(ICustomerRepository inner, ICacheStore cache) : ICustomerRepository
{
    private static readonly TimeSpan Ttl = TimeSpan.FromMinutes(10);

    public Task<Customer?> GetByIdAsync(int id, CancellationToken ct = default) => inner.GetByIdAsync(id, ct);

    public async Task<IReadOnlyList<Customer>> ListAsync(string tenantId, int skip, int take, CancellationToken ct = default)
    {
        var key = $"customers:{tenantId}:{skip}:{take}";
        var cached = await cache.GetAsync<IReadOnlyList<Customer>>(key, ct);
        if (cached is not null)
        {
            return cached;
        }

        var page = await inner.ListAsync(tenantId, skip, take, ct);
        await cache.SetAsync(key, page, Ttl, ct);
        return page;
    }

    public Task<int> CountAsync(string tenantId, CancellationToken ct = default) => inner.CountAsync(tenantId, ct);

    public async Task<IReadOnlyList<Customer>> SearchAsync(string tenantId, string term, CancellationToken ct = default)
    {
        var key = $"customers:search:{term}";
        var cached = await cache.GetAsync<IReadOnlyList<Customer>>(key, ct);
        if (cached is not null)
        {
            return cached;
        }

        var found = await inner.SearchAsync(tenantId, term, ct);
        await cache.SetAsync(key, found, Ttl, ct);
        return found;
    }

    public Task<Customer> AddAsync(Customer customer, CancellationToken ct = default) => inner.AddAsync(customer, ct);

    public Task UpdateAsync(Customer customer, CancellationToken ct = default) => inner.UpdateAsync(customer, ct);

    public Task<bool> DeleteAsync(int id, CancellationToken ct = default) => inner.DeleteAsync(id, ct);
}
