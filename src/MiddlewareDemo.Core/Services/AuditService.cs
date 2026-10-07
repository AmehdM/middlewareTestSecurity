using MiddlewareDemo.Core.Common;
using MiddlewareDemo.Core.Interfaces;
using MiddlewareDemo.Core.Models;

namespace MiddlewareDemo.Core.Services;

public interface IAuditService
{
    Task RecordAsync(string actor, string action, string target, string detail, CancellationToken ct = default);
    Task<PagedResult<AuditEntry>> ListAsync(PageRequest page, CancellationToken ct = default);
}

public sealed class AuditService : IAuditService
{
    private readonly IAuditRepository _audit;

    public AuditService(IAuditRepository audit)
    {
        _audit = audit;
    }

    public Task RecordAsync(string actor, string action, string target, string detail, CancellationToken ct = default) =>
        _audit.AddAsync(new AuditEntry { Actor = actor, Action = action, Target = target, Detail = detail }, ct);

    public async Task<PagedResult<AuditEntry>> ListAsync(PageRequest page, CancellationToken ct = default)
    {
        var items = await _audit.ListAsync(page.Skip, page.Take, ct);
        var total = await _audit.CountAsync(ct);
        return PagedResult<AuditEntry>.Create(items, page, total);
    }
}
