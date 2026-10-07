using Microsoft.Extensions.Logging.Abstractions;
using MiddlewareDemo.Core.Common;
using MiddlewareDemo.Core.Dtos;
using MiddlewareDemo.Core.Models;
using MiddlewareDemo.Core.Services;
using MiddlewareDemo.Tests.Support;

namespace MiddlewareDemo.Tests;

public sealed class CoreServiceTests
{
    private readonly FakeInventoryRepository _stock = new();

    private InventoryService CreateInventory() => new(_stock, NullLogger<InventoryService>.Instance);

    public CoreServiceTests()
    {
        _stock.Levels[1] = new StockLevel { ProductId = 1, Quantity = 5 };
        _stock.Levels[2] = new StockLevel { ProductId = 2, Quantity = 50 };
    }

    [Fact]
    public async Task ReserveAtomic_DecrementsWhenEnough()
    {
        var ok = await CreateInventory().ReserveAtomicAsync(1, 3);

        Assert.True(ok);
        Assert.Equal(2, _stock.Levels[1].Quantity);
    }

    [Fact]
    public async Task ReserveAtomic_FailsWhenNotEnough()
    {
        var ok = await CreateInventory().ReserveAtomicAsync(1, 9);

        Assert.False(ok);
        Assert.Equal(5, _stock.Levels[1].Quantity);
    }

    [Fact]
    public async Task Reserve_ReturnsFalseForUnknownProduct()
    {
        Assert.False(await CreateInventory().ReserveAsync(99, 1));
    }

    [Fact]
    public async Task Restock_IncreasesQuantity()
    {
        await CreateInventory().RestockAsync(2, 10);

        Assert.Equal(60, _stock.Levels[2].Quantity);
    }

    [Fact]
    public async Task LowStock_ListsOnlyProductsBelowThreshold()
    {
        var low = await CreateInventory().LowStockAsync(10);

        Assert.Single(low);
        Assert.Equal(1, low[0].ProductId);
    }

    [Fact]
    public async Task Notification_SendsMailAndStoresRecord()
    {
        var repository = new FakeNotificationRepository();
        var mail = new FakeMailSender();
        var service = new NotificationService(repository, mail, new FakeSmsSender());

        var saved = await service.SendAsync(new NotificationRequest("ana@example.test", "Hello", "Your order shipped", "mail"));

        Assert.Single(mail.Sent);
        Assert.Equal("ana@example.test", saved.Recipient);
        Assert.Single(await service.ListAsync("ana@example.test"));
    }

    [Fact]
    public async Task Notification_RoutesSmsChannelToSmsSender()
    {
        var sms = new FakeSmsSender();
        var mail = new FakeMailSender();
        var service = new NotificationService(new FakeNotificationRepository(), mail, sms);

        await service.SendAsync(new NotificationRequest("+50499887766", "Reminder", "Refill ready", "sms"));

        Assert.Single(sms.Sent);
        Assert.Empty(mail.Sent);
    }

    [Fact]
    public async Task Notification_RequiresRecipientAndMessage()
    {
        var service = new NotificationService(new FakeNotificationRepository(), new FakeMailSender(), new FakeSmsSender());

        await Assert.ThrowsAsync<RequestValidationException>(() =>
            service.SendAsync(new NotificationRequest("", "Subject", "", "mail")));
    }

    [Fact]
    public async Task Export_CreateJobRequiresDatasetAndFormat()
    {
        var service = new ExportService(new FakeExportRepository(), null!, null!);

        await Assert.ThrowsAsync<RequestValidationException>(() =>
            service.CreateJobAsync(new ExportRequest("", "csv"), "tester"));
    }

    [Fact]
    public async Task Export_CreateJobStoresRequest()
    {
        var jobs = new FakeExportRepository();
        var service = new ExportService(jobs, null!, null!);

        var job = await service.CreateJobAsync(new ExportRequest("products", "csv"), "tester");

        Assert.Equal("queued", job.Status);
        Assert.NotNull(await service.GetJobAsync(job.Id));
    }
}
