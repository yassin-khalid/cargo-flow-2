using CargoFlow.Domain;
using CargoFlow.Domain.Enums;

namespace CargoFlow.Services;

public interface INotificationService
{
    Notification CreateShipmentStatusChangedNotification(Shipment shipment, ShipmentStatusName shipmentStatusName);
    Notification CreateOfferSubmittedNotification(Shipment shipment);
    Notification CreateOfferAwardedNotification(Shipment shipment, Offer offer);
}

public class NotificationService : INotificationService
{

    public Notification CreateShipmentStatusChangedNotification(Shipment shipment, ShipmentStatusName shipmentStatusName)
    {
        return new()
        {
            Title = "Shipment Status Changed",
            Message = $"Shipment #{shipment.Id} status has been changed to {shipmentStatusName}",
            RecipientId = shipment.CustomerId,
            RecipientType = RecipientType.Customer,
            ShipmentId = shipment.Id,
            Type = NotificationType.ShipmentStatusChanged,
            CreatedAt = DateTime.UtcNow,
            ReadAt = null
        };
    }

    public Notification CreateOfferSubmittedNotification(Shipment shipment)
    {
        return new()
        {
            Title = "Offer Submitted",
            Message = $"New offer has been submitted successfully for shipment #{shipment.Id}",
            ShipmentId = shipment.Id,
            Type = NotificationType.OfferSubmitted,
            RecipientType = RecipientType.Customer,
            RecipientId = shipment.CustomerId,
            CreatedAt = DateTime.UtcNow,
            ReadAt = null,
        };
    }

    public Notification CreateOfferAwardedNotification(Shipment shipment, Offer offer)
    {
        return new()
        {
            Title = "Offer Awarded",
            Message = $"Offer #{offer.Id} for shipment {shipment.Id} has been awarded",
            RecipientId = offer.CompanyId,
            RecipientType = RecipientType.Company,
            ShipmentId = shipment.Id,
            Type = NotificationType.OfferAwarded,
            CreatedAt = DateTime.UtcNow,
            ReadAt = null
        };
    }
}
