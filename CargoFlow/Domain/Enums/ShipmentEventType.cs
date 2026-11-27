namespace CargoFlow.Domain.Enums;

public enum ShipmentEventType
{
    ShipmentPickedUp,
    ArrivedAtFacility,
    DepartedFacility,
    OutForDelivery,
    DeliveryAttemptFailed,
    Delivered,
    Delayed,
    Exception,
    Custom,
}
