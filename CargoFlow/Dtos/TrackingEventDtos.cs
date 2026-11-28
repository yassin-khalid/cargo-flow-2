using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using CargoFlow.Domain.Enums;
using CargoFlow.Helpers.Attributes.JsonConverters;
using CargoFlow.Helpers.Attributes.ValidationAttributes;

namespace CargoFlow.Dtos;

public record ShipmentTrackingEventDto(
    int Id,
    int ShipmentId,
    ShipmentEventType EventType,
    string Description,
    string? Location,
    decimal? Latitude,
    decimal? Longitude,
    DateTimeOffset OccurredAtUtc,
    DateTimeOffset CreatedAtUtc,
    int CreatedByUserId
);

[RequiredTogether(
    nameof(Latitude),
    nameof(Longitude),
    ErrorMessage = "Both Latitude and Longitude must be provided together, or both omitted"
)]
public sealed record ShipmentTrackingEventRequestDto
{
    [Required]
    [MaxLength(25, ErrorMessage = "Description must have 25 characters maximum")]
    [JsonPropertyName("description")]
    [JsonRequired]
    public required string Description { get; init; }

    [JsonPropertyName("location")]
    public string? Location { get; init; }

    [JsonPropertyName("latitude")]
    [Range(-90, 90, ErrorMessage = "Latitude must be a decimal value between -90 and 90")]
    public decimal? Latitude { get; init; }

    [JsonPropertyName("longitude")]
    [Range(-180, 180, ErrorMessage = "Longitude must be a decimal value between -180 and 180")]
    public decimal? Longitude { get; init; }

    [Required]
    [JsonPropertyName("event_type")]
    [JsonConverter(typeof(ShipmentEventTypeConverter))]
    [JsonRequired]
    public required ShipmentEventType EventType { get; init; }

    [Required]
    [JsonPropertyName("occurred_at")]
    [JsonConverter(typeof(StrictUtcDateTimeOffsetConverter))]
    [LessThanNow(ErrorMessage = "DateTime must be less than now")]
    [JsonRequired]
    public required DateTimeOffset OccurredAtUtc { get; init; }
};
