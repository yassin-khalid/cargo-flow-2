using CargoFlow.Data;
using CargoFlow.Domain;
using CargoFlow.Domain.Enums;

namespace CargoFlow.IntegrationTests.Helpers;

public static class NotificationSeeder
{
    public static async Task<List<Notification>> SeedNotifications(CargoFlowDbContext db, int count, RecipientType recipientType = RecipientType.Customer)
    {
        return recipientType switch
        {
            RecipientType.Company => await SeedCompanyNotificaitons(db, count),
            _ => await SeedCustomerNotification(db, count)
        };

    }

    private static async Task<List<Notification>> SeedCompanyNotificaitons(CargoFlowDbContext db, int count)
    {
        var (shipment, offers) = await ShipmentSeeder.SeedShipmentWithOffersAsync(db, ShipmentStatusName.OffersReceived, "Ton (t)", 100,
            [.. Enumerable.Range(1, count).Select(i => new OfferSpec("TestCompany", 100, OfferStateName.Submitted))]
        );

        List<Notification> notifications = [];
        for (var i = 0; i < count; i++)
        {
            var notification = new Notification()
            {
                ShipmentId = shipment.Id,
                Title = "Test Title",
                Message = "this is a test message",
                RecipientId = offers[i].CompanyId,
                RecipientType = RecipientType.Company,
                Type = NotificationType.OfferSubmitted,
                CreatedAt = DateTime.UtcNow,
                ReadAt = null
            };
            notifications.Add(notification);
        }

        db.Notifications.AddRange(notifications);
        await db.SaveChangesAsync();
        return notifications;
    }

    private static async Task<List<Notification>> SeedCustomerNotification(CargoFlowDbContext db, int count)
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
        foreach (var shipment in shipments)
        {
            var notification = new Notification()
            {
                ShipmentId = shipment.Id,
                RecipientId = shipment.CustomerId,
                RecipientType = RecipientType.Customer,
                Type = NotificationType.ShipmentStatusChanged,
                Title = "Test Title",
                Message = "Test Message",
                CreatedAt = DateTime.UtcNow,
                ReadAt = null,
            };
            notifications.Add(notification);
        }

        db.Notifications.AddRange(notifications);
        await db.SaveChangesAsync();
        return notifications;
    }

    public static async Task<List<Notification>> SeedNotificationsWithSharedRecipient(CargoFlowDbContext db, int count)
    {
        var (shipment, _) = await ShipmentSeeder.SeedShipmentWithOffersAsync(db, ShipmentStatusName.OffersReceived, "Ton (t)", 100,
                [
                        new("TestCompany", 100, OfferStateName.Submitted)
                ]);

        List<Notification> notifications = [];
        for (var i = 0; i < count; i++)
        {
            var notification = new Notification()
            {
                ShipmentId = shipment.Id,
                Title = "Test Title",
                Message = "this is a test message",
                RecipientId = shipment.Id,
                RecipientType = RecipientType.Customer,
                Type = NotificationType.OfferSubmitted,
                CreatedAt = DateTime.UtcNow,
                ReadAt = null
            };
            notifications.Add(notification);
        }

        db.Notifications.AddRange(notifications);
        await db.SaveChangesAsync();
        return notifications;
    }
}
