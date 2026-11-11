using CargoFlow.Data;
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
            .AddEndpointFilter<GetNotificationValidationFilter>();
        group.MapGet("/unread-count", GetUnreadNotificationCount)
            .AddEndpointFilter<MarkNotificationAsReadValidationFilter>();
        group.MapPatch("/{id:int}/read", MarkNotificationAsRead)
            .AddEndpointFilter<MarkNotificationAsReadValidationFilter>();
        group.MapPatch("/read-all", MarkAllNotificationsAsRead)
            .AddEndpointFilter<MarkNotificationAsReadValidationFilter>();
        return group;
    }

    private static async Task<Ok<List<NotificationDto>>> GetNotifications(CargoFlowDbContext db, [AsParameters] GetNotificationRequestDto dto)
    {
        var query = dto.ToQuery();
        var (recipientType,
            recipientId,
            shipmentId,
            notificationType,
            title,
            message,
            isRead) = query;

        var notifications = db.Notifications
            .AsNoTracking()
            .Where(n => n.RecipientType == recipientType && n.RecipientId == recipientId);

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

        notifications = notifications.OrderByDescending(n => n.CreatedAt);
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

    private static async Task<Ok<int>> GetUnreadNotificationCount(CargoFlowDbContext db, [AsParameters] MarkNotificationAsReadRequestDto dto)
    {
        var query = dto.ToQuery();
        var (recipientId, recipientType) = query;
        return TypedResults.Ok(await db.Notifications
            .AsNoTracking()
            .Where(n => n.ReadAt == null && n.RecipientId == query.RecipientId && n.RecipientType == query.RecipientType)
            .CountAsync());
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> MarkNotificationAsRead(int id, CargoFlowDbContext db, [AsParameters] MarkNotificationAsReadRequestDto dto)
    {
        var query = dto.ToQuery();
        var (recipientId, recipientType) = query;

        var notification = await db.Notifications
        .FirstOrDefaultAsync(n => n.Id == id && n.RecipientId == recipientId && n.RecipientType == recipientType);
        if (notification is null)
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Notification Not Found",
                detail: $"Notification #{id} does not exist"
            );
        }
        if (notification.ReadAt is not null)
        {
            return TypedResults.NoContent();
        }
        notification.ReadAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return TypedResults.NoContent();
    }

    private static async Task<NoContent> MarkAllNotificationsAsRead(CargoFlowDbContext db, [AsParameters] MarkNotificationAsReadRequestDto dto)
    {
        var query = dto.ToQuery();
        var (recipientId, recipientType) = query;

        var now = DateTime.UtcNow;
        await db.Notifications
           .Where(n => n.ReadAt == null && n.RecipientId == recipientId && n.RecipientType == recipientType)
           .ExecuteUpdateAsync(builder => builder
               .SetProperty(n => n.ReadAt, now));
        return TypedResults.NoContent();
    }
}
