using MiddlewareDemo.Core.Interfaces;
using MiddlewareDemo.Core.Models;

namespace MiddlewareDemo.Tests.Support;

public sealed class FakeInventoryRepository : IInventoryRepository
{
    public Dictionary<int, StockLevel> Levels { get; } = new();

    public Task<StockLevel?> GetStockAsync(int productId, CancellationToken ct = default) =>
        Task.FromResult(Levels.TryGetValue(productId, out var level) ? level : null);

    public Task DecrementAsync(int productId, int quantity, CancellationToken ct = default)
    {
        Levels[productId].Quantity -= quantity;
        return Task.CompletedTask;
    }

    public Task IncrementAsync(int productId, int quantity, CancellationToken ct = default)
    {
        Levels[productId].Quantity += quantity;
        return Task.CompletedTask;
    }

    public Task<bool> TryDecrementAsync(int productId, int quantity, CancellationToken ct = default)
    {
        if (!Levels.TryGetValue(productId, out var level) || level.Quantity < quantity)
        {
            return Task.FromResult(false);
        }

        level.Quantity -= quantity;
        return Task.FromResult(true);
    }

    public Task<IReadOnlyList<StockLevel>> ListBelowAsync(int threshold, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<StockLevel>>(Levels.Values.Where(l => l.Quantity < threshold).ToList());
}

public sealed class FakeNotificationRepository : INotificationRepository
{
    public List<Notification> Stored { get; } = new();

    public Task<Notification> AddAsync(Notification notification, CancellationToken ct = default)
    {
        notification.Id = Stored.Count + 1;
        Stored.Add(notification);
        return Task.FromResult(notification);
    }

    public Task<IReadOnlyList<Notification>> ListAsync(string recipient, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<Notification>>(Stored.Where(n => n.Recipient == recipient).ToList());
}

public sealed class FakeMailSender : IMailSender
{
    public List<(string To, string Subject, string Body)> Sent { get; } = new();

    public Task SendAsync(string to, string subject, string htmlBody, CancellationToken ct = default)
    {
        Sent.Add((to, subject, htmlBody));
        return Task.CompletedTask;
    }
}

public sealed class FakeSmsSender : ISmsSender
{
    public List<(string Phone, string Text)> Sent { get; } = new();

    public Task SendAsync(string phone, string text, CancellationToken ct = default)
    {
        Sent.Add((phone, text));
        return Task.CompletedTask;
    }
}

public sealed class FakeExportRepository : IExportRepository
{
    private readonly Dictionary<int, ExportJob> _jobs = new();

    public Task<ExportJob> AddAsync(ExportJob job, CancellationToken ct = default)
    {
        job.Id = _jobs.Count + 1;
        _jobs[job.Id] = job;
        return Task.FromResult(job);
    }

    public Task<ExportJob?> GetAsync(int id, CancellationToken ct = default) =>
        Task.FromResult(_jobs.TryGetValue(id, out var job) ? job : null);

    public Task UpdateAsync(ExportJob job, CancellationToken ct = default)
    {
        _jobs[job.Id] = job;
        return Task.CompletedTask;
    }
}
