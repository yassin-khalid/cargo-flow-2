using CargoFlow.Data;
using CargoFlow.Domain.Enums;
using CargoFlow.Dtos;
using CargoFlow.Endpoints.Filters;
using CargoFlow.Extensions;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace CargoFlow.Endpoints;

public static class NotificationEndpoints
{
    public static RouteGroupBuilder MapNotificationsEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/", GetNotifications)
            .AddEndpointFilter<NotificationValidationFilter>();
        return group;
    }

    private static async Task<Ok<List<NotificationDto>>> GetNotifications(CargoFlowDbContext db, [AsParameters] GetNotificationRequestDto dto)
    {
        Console.WriteLine($"dtoooooooooooooooooooooooooooooooooooooooo: {dto}");

        var query = dto.ToQuery();
        var (recipientType,
            recipientId,
            shipmentId,
            notificationType,
            title,
            message,
            isRead) = query;

        var notifications = db.Notifications.AsNoTracking();

        if (recipientType is not null)
        {
            notifications = notifications.Where(n => n.RecipientType == recipientType);
        }
        if (recipientId is not null)
        {
            notifications = notifications.Where(n => n.RecipientId == recipientId);
        }
        if (notificationType is not null)
        {
            notifications = notifications.Where(n => n.Type == notificationType);
        }
        if (message is not null)
        {
            notifications = notifications.Where(n => n.Message.Contains(message));
        }
        if (title is not null)
        {
            notifications = notifications.Where(n => n.Title.Contains(title));
        }
        if (shipmentId is not null)
        {
            notifications = notifications.Where(n => n.ShipmentId == shipmentId);
        }
        if (isRead is { } read)
        {
            notifications = notifications.Where(n => read ? n.ReadAt != null : n.ReadAt == null);
        }

        var result = await notifications.Select(n => new NotificationDto(n.Id,
            n.RecipientType.ToString(),
            n.RecipientId,
            n.ShipmentId,
            n.Type.ToString(),
            n.Title,
            n.Message,
            n.CreatedAt,
            n.ReadAt != null))
        .ToListAsync();

        return TypedResults.Ok(result);
    }
}
