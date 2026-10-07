using MiddlewareDemo.Core.Common;
using MiddlewareDemo.Core.Interfaces;
using MiddlewareDemo.Core.Models;

namespace MiddlewareDemo.Core.Services;

public interface ITenantService
{
    Task<Tenant?> GetAsync(string id, CancellationToken ct = default);
    Task<IReadOnlyList<Tenant>> ListAsync(CancellationToken ct = default);
    Task<Tenant> CreateAsync(string id, string name, CancellationToken ct = default);
}

public sealed class TenantService : ITenantService
{
    private readonly ITenantRepository _tenants;

    public TenantService(ITenantRepository tenants)
    {
        _tenants = tenants;
    }

    public Task<Tenant?> GetAsync(string id, CancellationToken ct = default) => _tenants.GetByIdAsync(id, ct);

    public Task<IReadOnlyList<Tenant>> ListAsync(CancellationToken ct = default) => _tenants.ListAsync(ct);

    public async Task<Tenant> CreateAsync(string id, string name, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(name))
        {
            throw new RequestValidationException("Tenant id and name are required");
        }

        if (await _tenants.GetByIdAsync(id, ct) is not null)
        {
            throw new RequestValidationException("Tenant already exists");
        }

        return await _tenants.AddAsync(new Tenant { Id = id, Name = name }, ct);
    }
}
