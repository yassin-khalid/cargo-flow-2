using CargoFlow.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace CargoFlow.Dtos;

public record NotificationDto(
    int Id,
    string RecipientType,
    int RecipientId,
    int ShipmentId,
    string NotificationType,
    string Title,
    string Message,
    DateTime CreatedAt,
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
