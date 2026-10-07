using Microsoft.Extensions.Logging;
using MiddlewareDemo.Core.Common;
using MiddlewareDemo.Core.Dtos;
using MiddlewareDemo.Core.Interfaces;
using MiddlewareDemo.Core.Logging;
using MiddlewareDemo.Core.Models;
using MiddlewareDemo.Core.Validation;

namespace MiddlewareDemo.Core.Services;

public interface ICustomerService
{
    Task<CustomerDto?> GetAsync(int id, int requesterId, bool isAdmin, CancellationToken ct = default);
    Task<PagedResult<CustomerDto>> ListAsync(string tenantId, PageRequest page, CancellationToken ct = default);
    Task<IReadOnlyList<CustomerDto>> SearchAsync(string tenantId, string term, CancellationToken ct = default);
    Task<CustomerDto> CreateAsync(CreateCustomerRequest request, int ownerId, CancellationToken ct = default);
    Task<CustomerDto?> UpdateAsync(int id, UpdateCustomerRequest request, CancellationToken ct = default);
    Task<bool> DeleteAsync(int id, CancellationToken ct = default);
}

public sealed class CustomerService : ICustomerService
{
    private readonly ICustomerRepository _customers;
    private readonly ICacheStore _cache;
    private readonly IAuditService _audit;
    private readonly ILogger<CustomerService> _logger;

    public CustomerService(ICustomerRepository customers, ICacheStore cache, IAuditService audit, ILogger<CustomerService> logger)
    {
        _customers = customers;
        _cache = cache;
        _audit = audit;
        _logger = logger;
    }

    public async Task<CustomerDto?> GetAsync(int id, int requesterId, bool isAdmin, CancellationToken ct = default)
    {
        var customer = await _customers.GetByIdAsync(id, ct);
        if (customer is null || (!isAdmin && customer.OwnerUserId != requesterId))
        {
            return null;
        }

        return Mappings.ToDto(customer);
    }

    public async Task<PagedResult<CustomerDto>> ListAsync(string tenantId, PageRequest page, CancellationToken ct = default)
    {
        var rows = await _customers.ListAsync(tenantId, page.Skip, page.Take, ct);
        var total = await _customers.CountAsync(tenantId, ct);
        return PagedResult<CustomerDto>.Create(rows.Select(Mappings.ToDto).ToList(), page, total);
    }

    public async Task<IReadOnlyList<CustomerDto>> SearchAsync(string tenantId, string term, CancellationToken ct = default)
    {
        var key = $"customers:search:{term}";
        var cached = await _cache.GetAsync<List<CustomerDto>>(key, ct);
        if (cached is not null)
        {
            return cached;
        }

        var rows = await _customers.SearchAsync(tenantId, term, ct);
        var result = rows.Select(Mappings.ToDto).ToList();
        await _cache.SetAsync(key, result, TimeSpan.FromMinutes(5), ct);
        return result;
    }

    public async Task<CustomerDto> CreateAsync(CreateCustomerRequest request, int ownerId, CancellationToken ct = default)
    {
        _logger.LogInformation("{Request}", RequestLogHelper.Describe("customer.create", request));
        CustomerValidator.Validate(request).ThrowIfInvalid();

        var created = await _customers.AddAsync(new Customer
        {
            OwnerUserId = ownerId,
            Name = request.Name.Trim(),
            Email = request.Email.Trim(),
            Phone = request.Phone,
            NationalId = request.NationalId,
            TenantId = request.TenantId,
        }, ct);

        await _audit.RecordAsync(ownerId.ToString(), "customer.create", created.Id.ToString(), string.Empty, ct);
        return Mappings.ToDto(created);
    }

    public async Task<CustomerDto?> UpdateAsync(int id, UpdateCustomerRequest request, CancellationToken ct = default)
    {
        var customer = await _customers.GetByIdAsync(id, ct);
        if (customer is null)
        {
            return null;
        }

        customer.Name = request.Name;
        customer.Email = request.Email;
        customer.Phone = request.Phone;
        await _customers.UpdateAsync(customer, ct);
        return Mappings.ToDto(customer);
    }

    public Task<bool> DeleteAsync(int id, CancellationToken ct = default) => _customers.DeleteAsync(id, ct);
}
