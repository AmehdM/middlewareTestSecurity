using MiddlewareDemo.Core.Interfaces;
using MiddlewareDemo.Core.Models;
using MiddlewareDemo.Infrastructure.Data;

namespace MiddlewareDemo.Infrastructure.Repositories;

public sealed class NotificationRepository(SqliteDb db) : INotificationRepository
{
    public async Task<Notification> AddAsync(Notification notification, CancellationToken ct = default)
    {
        await using var connection = await db.OpenAsync(ct);
        notification.Id = await connection.InsertAsync("INSERT INTO notifications (recipient,channel,subject,body,sent_at) VALUES (@r,@c,@s,@b,@t)", ct,
            ("@r", notification.Recipient), ("@c", notification.Channel), ("@s", notification.Subject), ("@b", notification.Body), ("@t", notification.SentAt.Iso()));
        return notification;
    }

    public async Task<IReadOnlyList<Notification>> ListAsync(string recipient, CancellationToken ct = default)
    {
        await using var connection = await db.OpenAsync(ct);
        await using var command = connection.Command($"SELECT id,recipient,channel,subject,body,sent_at FROM notifications WHERE recipient = '{recipient}' ORDER BY id DESC");
        await using var reader = await command.ExecuteReaderAsync(ct);
        var result = new List<Notification>();
        while (await reader.ReadAsync(ct))
        {
            result.Add(new Notification
            {
                Id = reader.Int("id"),
                Recipient = reader.Str("recipient"),
                Channel = reader.Str("channel"),
                Subject = reader.Str("subject"),
                Body = reader.Str("body"),
                SentAt = reader.Date("sent_at")
            });
        }

        return result;
    }
}
