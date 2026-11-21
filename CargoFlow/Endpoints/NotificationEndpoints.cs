using System.Security.Claims;
using CargoFlow.Data;
using CargoFlow.Dtos;
using CargoFlow.Endpoints.Filters;
using CargoFlow.Extensions;
using CargoFlow.Helpers;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace CargoFlow.Endpoints;

public static class NotificationEndpoints
{
    public static RouteGroupBuilder MapNotificationsEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/", GetNotifications)
            .AddEndpointFilter<GetNotificationValidationFilter>();
        group.MapGet("/unread-count", GetUnreadNotificationCount);
        group.MapPatch("/{id:int}/read", MarkNotificationAsRead);
        group.MapPatch("/read-all", MarkAllNotificationsAsRead);
        return group;
    }

    private static async Task<Results<Ok<List<NotificationDto>>, ProblemHttpResult>> GetNotifications(
        CargoFlowDbContext db,
        [AsParameters] GetNotificationRequestDto dto,
        IRecipientsReferenceData recipientsReferenceData,
        ClaimsPrincipal user
    )
    {
        var recipientId = recipientsReferenceData.GetRecipientId(user);

        if (recipientId is null)
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status403Forbidden,
                title: "Missing Claims Issue",
                detail: "Missing necessary claims to perform this endpoint"
            );
        }

        var query = dto.ToQuery();
        var (
            shipmentId,
            notificationType,
            title,
            message,
            isRead) = query;

        var notifications = db.Notifications
            .AsNoTracking()
            .Where(n => n.RecipientId == recipientId);

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
            n.ShipmentId,
            n.Type.ToString(),
            n.Title,
            n.Message,
            n.CreatedAt,
            n.ReadAt != null))
        .ToListAsync();

        return TypedResults.Ok(result);
    }

    private static async Task<Results<ProblemHttpResult, Ok<int>>> GetUnreadNotificationCount(
        CargoFlowDbContext db,
        IRecipientsReferenceData recipientsReferenceData,
        ClaimsPrincipal user
    )
    {
        var recipientId = recipientsReferenceData.GetRecipientId(user);

        if (recipientId is null)
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status403Forbidden,
                title: "Missing Claims Issue",
                detail: "Missing necessary claims to perform this endpoint"
            );
        }
        return TypedResults.Ok(await db.Notifications
            .AsNoTracking()
            .Where(n => n.ReadAt == null
                && n.RecipientId == recipientId
            )
            .CountAsync()
        );
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> MarkNotificationAsRead(
        int id,
        CargoFlowDbContext db,
        IRecipientsReferenceData recipientsReferenceData,
        ClaimsPrincipal user
    )
    {
        var recipientId = recipientsReferenceData.GetRecipientId(user);

        if (recipientId is null)
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status403Forbidden,
                title: "Missing Claims Issue",
                detail: "Missing necessary claims to perform this endpoint"
            );
        }

        var notification = await db.Notifications
        .FirstOrDefaultAsync(n => n.Id == id
            && n.RecipientId == recipientId);
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

    private static async Task<Results<NoContent, ProblemHttpResult>> MarkAllNotificationsAsRead(
        CargoFlowDbContext db,
        IRecipientsReferenceData recipientsReferenceData,
        ClaimsPrincipal user
    )
    {
        var recipientId = recipientsReferenceData.GetRecipientId(user);

        if (recipientId is null)
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status403Forbidden,
                title: "Missing Claims Issue",
                detail: "Missing necessary claims to perform this endpoint"
            );
        }

        var now = DateTime.UtcNow;
        await db.Notifications
           .Where(n => n.ReadAt == null
               && n.RecipientId == recipientId)
           .ExecuteUpdateAsync(builder => builder
               .SetProperty(n => n.ReadAt, now)
           );
        return TypedResults.NoContent();
    }
}
