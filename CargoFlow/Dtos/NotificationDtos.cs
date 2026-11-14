using System.Text.Json.Serialization;
using CargoFlow.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace CargoFlow.Dtos;

public record NotificationDto(
    [property: JsonPropertyName("id")]
    int Id,
    [property: JsonPropertyName("recipient_type")]
    string RecipientType,
    [property: JsonPropertyName("recipient_id")]
    int RecipientId,
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
    [FromQuery(Name = "recipient_type")]
    string RecipientType,
    [FromQuery(Name = "recipient_id")]
    int RecipientId,
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

public record MarkNotificationAsReadRequestDto(
    [FromQuery(Name = "recipient_type")]
    string RecipientType,
    [FromQuery(Name = "recipient_id")]
    int RecipientId);

public record MarkNotificationAsReadQuery(
    int RecipientId,
    RecipientType RecipientType);

public record GetNotificationQuery(
    RecipientType RecipientType,
    int RecipientId,
    int? ShipmentId,
    NotificationType? NotificationType,
    string? Title,
    string? Message,
    bool? IsRead);
