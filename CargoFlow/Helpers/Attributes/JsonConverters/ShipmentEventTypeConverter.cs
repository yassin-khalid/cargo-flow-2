using System.Text.Json;
using System.Text.Json.Serialization;
using CargoFlow.Domain.Enums;

namespace CargoFlow.Helpers.Attributes.JsonConverters;

public sealed class ShipmentEventTypeConverter : JsonConverter<ShipmentEventType>
{
    private static readonly Dictionary<string, ShipmentEventType> _allowedMap = new(
        StringComparer.OrdinalIgnoreCase
    )
    {
        ["picked_up"] = ShipmentEventType.ShipmentPickedUp,
        ["arrived"] = ShipmentEventType.ArrivedAtFacility,
        ["departed"] = ShipmentEventType.DepartedFacility,
        ["out_for_delivery"] = ShipmentEventType.OutForDelivery,
        ["delivery_failed"] = ShipmentEventType.DeliveryAttemptFailed,
        ["delivered"] = ShipmentEventType.Delivered,
        ["delayed"] = ShipmentEventType.Delayed,
        ["exception"] = ShipmentEventType.Exception,
        ["custom"] = ShipmentEventType.Custom,
    };

    private static readonly Dictionary<ShipmentEventType, string> _reversedMap = new()
    {
        [ShipmentEventType.ShipmentPickedUp] = "picked_up",
        [ShipmentEventType.ArrivedAtFacility] = "arrived",
        [ShipmentEventType.DepartedFacility] = "departed",
        [ShipmentEventType.OutForDelivery] = "out_for_delivery",
        [ShipmentEventType.DeliveryAttemptFailed] = "delivery_failed",
        [ShipmentEventType.Delivered] = "delivered",
        [ShipmentEventType.Delayed] = "delayed",
        [ShipmentEventType.Exception] = "exception",
        [ShipmentEventType.Custom] = "custom",
    };

    public override ShipmentEventType Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options
    )
    {
        if (reader.TokenType != JsonTokenType.String)
        {
            throw new JsonException("Event type  must be provided as string");
        }

        var raw = reader.GetString()?.Trim();
        if (string.IsNullOrEmpty(raw) || !_allowedMap.TryGetValue(raw, out var eventType))
        {
            throw new JsonException(
                $"Invalid event type '{raw}'. Allowed values: '{string.Join(", ", _allowedMap.Keys)}"
            );
        }

        return eventType;
    }

    public override void Write(
        Utf8JsonWriter writer,
        ShipmentEventType value,
        JsonSerializerOptions options
    )
    {
        if (!_reversedMap.TryGetValue(value, out var eventTypeStr))
        {
            throw new JsonException($"Unsuppored ShipmentEventType value '{value}'");
        }

        writer.WriteStringValue(eventTypeStr);
    }
}
