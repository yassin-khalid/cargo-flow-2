using CargoFlow.Data;
using CargoFlow.Domain;
using CargoFlow.Domain.Enums;

namespace CargoFlow.IntegrationTests.Helpers;

public enum NotificationReadStatus
{
    Read,
    Unread,
    ReadUnread
}

public static class NotificationSeeder
{
    public static async Task<List<Notification>> SeedNotifications(
        CargoFlowDbContext db,
        int count,
        RecipientType recipientType = RecipientType.Customer,
        NotificationReadStatus readStatus = NotificationReadStatus.Unread
    )
    {
        return recipientType switch
        {
            RecipientType.Company => await SeedCompanyNotificaitons(db, count, readStatus),
            _ => await SeedCustomerNotification(db, count, readStatus)
        };

    }

    private static async Task<List<Notification>> SeedCompanyNotificaitons(
        CargoFlowDbContext db,
        int count,
        NotificationReadStatus readStatus
    )
    {
        var (shipment, offers) = await ShipmentSeeder.SeedShipmentWithOffersAsync(db, ShipmentStatusName.OffersReceived, "Ton (t)", 100,
            [.. Enumerable.Range(1, count).Select(i => new OfferSpec("TestCompany", 100, OfferStateName.Submitted))]
        );

        List<Notification> notifications = [];
        var now = DateTime.UtcNow;
        for (var i = 0; i < count; i++)
        {
            DateTime? readAt = readStatus switch
            {
                NotificationReadStatus.Read => now.AddHours((i + 1) * -1),
                NotificationReadStatus.Unread => null,
                _ => i % 2 == 0 ? now.AddHours((i + 1) * -1) : null
            };
            var notification = new Notification()
            {
                ShipmentId = shipment.Id,
                Title = "Test Title",
                Message = "this is a test message",
                RecipientId = offers[i].CompanyId,
                RecipientType = RecipientType.Company,
                Type = NotificationType.OfferSubmitted,
                CreatedAt = now.AddDays((i + 1) * -1),
                ReadAt = readAt
            };
            notifications.Add(notification);
        }

        db.Notifications.AddRange(notifications);
        await db.SaveChangesAsync();
        return notifications;
    }

    private static async Task<List<Notification>> SeedCustomerNotification(
        CargoFlowDbContext db,
        int count,
        NotificationReadStatus readStatus
    )
    {
        List<Shipment> shipments = [];
        for (var i = 0; i < count; i++)
        {
            var completedShipment = await ShipmentSeeder.SeedShipmentListWithUniqueStatusAsync(db, [
               new(ShipmentStatusName.Completed, "Ton (t)", 100)
            ]);
            shipments.Add(completedShipment.Values.First());
        }

        List<Notification> notifications = [];
        var counter = 0;
        var now = DateTime.UtcNow;
        foreach (var shipment in shipments)
        {
            counter++;
            DateTime? readAt = readStatus switch
            {
                NotificationReadStatus.Read => now.AddHours((counter + 1) * -1),
                NotificationReadStatus.Unread => null,
                _ => counter % 2 == 0 ? now.AddHours((counter + 1) * -1) : null
            };
            var notification = new Notification()
            {
                ShipmentId = shipment.Id,
                RecipientId = shipment.CustomerId,
                RecipientType = RecipientType.Customer,
                Type = NotificationType.ShipmentStatusChanged,
                Title = "Test Title",
                Message = "Test Message",
                CreatedAt = now.AddDays((counter + 1) * -1),
                ReadAt = readAt
            };
            notifications.Add(notification);
        }

        db.Notifications.AddRange(notifications);
        await db.SaveChangesAsync();
        return notifications;
    }

    public static async Task<List<Notification>> SeedNotificationsWithSharedRecipient(
        CargoFlowDbContext db,
        int count,
        NotificationReadStatus readStatus = NotificationReadStatus.Unread
    )
    {
        var (shipment, _) = await ShipmentSeeder.SeedShipmentWithOffersAsync(db, ShipmentStatusName.OffersReceived, "Ton (t)", 100,
                [
                        new("TestCompany", 100, OfferStateName.Submitted)
                ]);

        List<Notification> notifications = [];
        var now = DateTime.UtcNow;
        for (var i = 0; i < count; i++)
        {
            DateTime? readAt = readStatus switch
            {
                NotificationReadStatus.Read => now.AddHours((i + 1) * -1),
                NotificationReadStatus.Unread => null,
                _ => i % 2 == 0 ? now.AddHours((i + 1) * -1) : null
            };
            var notification = new Notification()
            {
                ShipmentId = shipment.Id,
                Title = "Test Title",
                Message = "this is a test message",
                RecipientId = shipment.Id,
                RecipientType = RecipientType.Customer,
                Type = NotificationType.OfferSubmitted,
                CreatedAt = now.AddDays((i + 1) * -1),
                ReadAt = readAt
            };
            notifications.Add(notification);
        }

        db.Notifications.AddRange(notifications);
        await db.SaveChangesAsync();
        return notifications;
    }
}
