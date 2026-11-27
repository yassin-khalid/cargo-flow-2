using System.Security.Claims;
using CargoFlow.Data;
using CargoFlow.Domain;
using CargoFlow.Dtos;
using CargoFlow.Helpers;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace CargoFlow.Endpoints;

public static class TrackingEventEndpoints
{
    public static RouteGroupBuilder MapTrackingEventEndpoints(this RouteGroupBuilder group)
    {
        group
            .MapPost("/{shipmentId:int}/tracking-events", CreateTrackingEvent)
            .RequireAuthorization(policy => policy.RequireRole("Comapny"));
        group.MapGet("/{shipmentId:int}/tracking-events", GetShipmentTrackingEvents);
        return group;
    }

    private static async Task<Results<Created, ProblemHttpResult>> CreateTrackingEvent(
        CargoFlowDbContext db,
        ShipmentTrackingEventRequestDto request,
        int shipmentId,
        TimeProvider timeProvider,
        ClaimsPrincipal user,
        IRecipientsReferenceData recipientsReferenceData,
        CancellationToken cancellationToken = default
    )
    {
        var companyId = recipientsReferenceData.GetRecipientId(user);

        if (companyId is not int userId)
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Compnay profile not found",
                detail: "Couldn't find company profile assosiated with the current user"
            );
        }

        var shipment = await db
            .Shipments.Include(s => s.Offers)
            .FirstOrDefaultAsync(s => s.Id == shipmentId, cancellationToken);

        if (shipment is null)
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Shipment not found",
                detail: $"Shipment #{shipmentId} does not exist"
            );
        }

        var awardedOffer = shipment.Offers.FirstOrDefault(o => o.HasWon);
        if (awardedOffer is null || awardedOffer.CompanyId != companyId)
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status403Forbidden,
                title: "Unauthorized Access",
                detail: $"You're not authorized to create event in shipment #{shipmentId}"
            );
        }

        var now = timeProvider.GetUtcNow();
        var trackingEvent = new ShipmentTrackingEvent()
        {
            ShipmentId = shipmentId,
            EventType = request.EventType,
            Description = request.Description,
            Location = request.Location,
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            CreatedAtUtc = now,
            CreatedByUserId = userId,
            OccuredAtUtc = request.OccuredAtUtc,
        };

        db.ShipmentTrackingEvents.Add(trackingEvent);
        await db.SaveChangesAsync(cancellationToken);

        return TypedResults.Created();
    }

    private static async Task<
        Results<Ok<List<ShipmentTrackingEventDto>>, ProblemHttpResult>
    > GetShipmentTrackingEvents(
        CargoFlowDbContext db,
        RecipientsReferenceData recipientsReferenceData,
        int shipmentId,
        ClaimsPrincipal user
    )
    {
        var userId = recipientsReferenceData.GetRecipientId(user);

        if (userId is null)
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status401Unauthorized,
                title: "User profile not found",
                detail: "Unable to find user profile assosiated with this user"
            );
        }

        var userType = user.FindFirst("user_type")?.Value;

        var shipment = await db
            .Shipments.Include(s => s.ShipmentTrackingEvents)
            .Include(s => s.Offers)
            .FirstOrDefaultAsync(s => s.Id == shipmentId);

        if (shipment is null)
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Shipment not found",
                detail: $"Couldn't find shipment #{shipmentId}"
            );
        }

        if (userType is "Customer" && shipment.CustomerId != userId)
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status403Forbidden,
                title: "Unauthorized access",
                detail: $"You're not authorized to get tracking events of shipment #{shipmentId}, it belongs to another user"
            );
        }

        var awardedOffer = shipment.Offers.FirstOrDefault(o => o.HasWon);
        if (awardedOffer is null || awardedOffer.CompanyId != userId)
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status403Forbidden,
                title: "Unauthorized Access",
                detail: $"You're not authorized to create event in shipment #{shipmentId}"
            );
        }

        return TypedResults.Ok(
            shipment
                .ShipmentTrackingEvents.Select(s => new ShipmentTrackingEventDto(
                    s.Id,
                    s.ShipmentId,
                    s.Description,
                    s.Location,
                    s.Latitude,
                    s.Longitude,
                    s.OccuredAtUtc,
                    s.CreatedAtUtc,
                    s.CreatedByUserId
                ))
                .ToList()
        );
    }
}
