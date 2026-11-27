using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using CargoFlow.Domain.Enums;
using CargoFlow.Helpers.Attributes.JsonConverters;
using CargoFlow.Helpers.Attributes.ValidationAttributes;

namespace CargoFlow.Dtos;

public record ShipmentTrackingEventDto(
    int Id,
    int ShipmentId,
    string Description,
    string? Location,
    decimal? Latitude,
    decimal? Longitude,
    DateTimeOffset OccuredAtUtc,
    DateTimeOffset CreatedAtUtc,
    int CreatedByUserId
);

[RequiredTogether(
    nameof(Latitude),
    nameof(Longitude),
    ErrorMessage = "Both Latitude and Longitude must be provided together, or both omitted"
)]
public record ShipmentTrackingEventRequestDto(
    [property: JsonPropertyName("description")] string Description,
    [property: JsonPropertyName("location")] string? Location,
    [property: JsonPropertyName("latitude")]
    [Range(-90, 90, ErrorMessage = "Latitude must be a decimal value between -90 and 90")]
        decimal? Latitude,
    [property: JsonPropertyName("longitude")]
    [Range(-180, 180, ErrorMessage = "Longitude must be a decimal value between -180 and 180")]
        decimal? Longitude,
    [property: JsonPropertyName("event_type")]
    [property: JsonConverter(typeof(ShipmentEventTypeConverter))]
        ShipmentEventType EventType,
    [property: JsonPropertyName("occured_at")]
    [property: JsonConverter(typeof(StrictUtcDateTimeOffsetConverter))]
    [LessThanNow(ErrorMessage = "DateTime must be less than now")]
        DateTimeOffset OccuredAtUtc
);
