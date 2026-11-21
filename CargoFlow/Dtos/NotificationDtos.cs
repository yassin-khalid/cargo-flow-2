using System.Text.Json.Serialization;
using CargoFlow.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace CargoFlow.Dtos;

public record NotificationDto(
    [property: JsonPropertyName("id")]
    int Id,
    [property: JsonPropertyName("shipment_id")]
    int ShipmentId,
    [property: JsonPropertyName("notification_type")]
    string NotificationType,
    [property: JsonPropertyName("title")]
    string Title,
    [property: JsonPropertyName("message")]
    string Message,
    [property: JsonPropertyName("created_at")]
    DateTime CreatedAt,
    [property: JsonPropertyName("is_read")]
    bool IsRead);

public record GetNotificationRequestDto(
    [FromQuery(Name = "shipment_id")]
    int? ShipmentId,
    [FromQuery(Name = "notification_type")]
    string? NotificationType,
    [FromQuery(Name = "title")]
    string? Title,
    [FromQuery(Name = "message")]
    string? Message,
    [FromQuery(Name = "is_read")]
    bool? IsRead);

public record MarkNotificationAsReadQuery(
    int RecipientId,
    RecipientType RecipientType);

public record GetNotificationQuery(
    int? ShipmentId,
    NotificationType? NotificationType,
    string? Title,
    string? Message,
    bool? IsRead);
