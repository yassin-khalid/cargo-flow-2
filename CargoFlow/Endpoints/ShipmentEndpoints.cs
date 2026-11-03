using CargoFlow.Data;
using CargoFlow.Domain;
using CargoFlow.Domain.Enums;
using CargoFlow.Dtos;
using CargoFlow.Helpers;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace CargoFlow.Endpoints;

public static class ShipmentEndpoints
{
    public static RouteGroupBuilder MapShipmentEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/{id:int}", GetShipment)
            .WithName(EndpointRouteNames.GetShipmentByIdRoute);
        group.MapPost("/", CreateShipment);
        group.MapPost("/{id:int}/open-for-offers", OpenForOffers);
        group.MapPost("/{id:int}/cancel", CancelShipment);
        group.MapPost("/{id:int}/start", StartShipment);
        group.MapPost("/{id:int}/deliver", DeliverShipment);
        return group;
    }

    private static async Task<Results<Ok<ShipmentDto>, NotFound>> GetShipment(int id, CargoFlowDbContext db)
    {
        return await db.Shipments
            .AsNoTracking()
            .Where(s => s.Id == id)
            .Select(s => new ShipmentDto(s.Id, s.CustomerId, s.PickupId, s.DestinationId, s.Weight, s.WeightTypeId, s.ShipmentStatusId))
            .FirstOrDefaultAsync() switch
        {
            { } dto => TypedResults.Ok(dto),
            _ => TypedResults.NotFound()
        };
    }

    private static async Task<Results<CreatedAtRoute<ShipmentDto>, ValidationProblem, ProblemHttpResult>> CreateShipment(CargoFlowDbContext db, CreateShipmentRequestDto dto, IReferenceData referenceData)
    {
        var isCustomerExists = await db.Customers.AnyAsync(c => c.Id == dto.CustomerId);
        if (!isCustomerExists)
        {
            return HttpResultProblems.ValidationProblem("customer_id", ["customer_id not exists"]);
        }

        var isPickupAddressExists = await db.Addresses.AnyAsync(a => a.Id == dto.PickupId);
        if (!isPickupAddressExists)
        {
            return HttpResultProblems.ValidationProblem("pickup_id", ["pickup_id not exists"]);
        }

        var isDestinationAddressExists = await db.Addresses.AnyAsync(a => a.Id == dto.DestinationId);
        if (!isDestinationAddressExists)
        {
            return HttpResultProblems.ValidationProblem("destination_id", ["destination_id not exists"]);
        }

        var isWeightTypeExists = await db.WeightTypes.AnyAsync(w => w.Id == dto.WeightTypeId);
        if (!isWeightTypeExists)
        {
            return HttpResultProblems.ValidationProblem("weight_type_id", ["weight_type_id not exists"]);
        }

        var draftShipmentId = referenceData.GetShipmentStatusId(ShipmentStatusName.Draft);

        var shipment = new Shipment()
        {
            CustomerId = dto.CustomerId,
            DestinationId = dto.DestinationId,
            PickupId = dto.PickupId,
            WeightTypeId = dto.WeightTypeId,
            ShipmentStatusId = draftShipmentId,
            Weight = dto.Weight,
            ShipmentStatusHistories = [
                new ShipmentStatusHistory()
                {
                    ShipmentStatusId = draftShipmentId,
                    DateTime = DateTime.UtcNow                   
                }
            ]
        };

        db.Shipments.Add(shipment);
        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (ExceptionsHelpers.IsForeignKeyViolation(ex, "FK_Shipments_Customers_CustomerId"))
        {
            return HttpResultProblems.ValidationProblem("customer_id", ["customer_id not exists"]);
        }
        catch (DbUpdateException ex) when (ExceptionsHelpers.IsForeignKeyViolation(ex, "FK_Shipments_Addresses_PickupId"))
        {
            return HttpResultProblems.ValidationProblem("pickup_id", ["pickup_id not exists"]);
        }
        catch (DbUpdateException ex) when (ExceptionsHelpers.IsForeignKeyViolation(ex, "FK_Shipments_Addresses_DestinationId"))
        {
            return HttpResultProblems.ValidationProblem("destination_id", ["destination_id not exists"]);
        }
        catch (DbUpdateException ex) when (ExceptionsHelpers.IsForeignKeyViolation(ex, "FK_Shipments_WeightTypes_WeightTypeId"))
        {
            return HttpResultProblems.ValidationProblem("weight_type_id", ["weight_type_id not exists"]);
        }


        return TypedResults.CreatedAtRoute(value: new ShipmentDto(shipment.Id, shipment.CustomerId, shipment.PickupId, shipment.DestinationId, shipment.Weight, shipment.WeightTypeId, shipment.ShipmentStatusId), routeName: EndpointRouteNames.GetShipmentByIdRoute, routeValues: new { id = shipment.Id });
    }


    private static async Task<Results<NoContent, ProblemHttpResult>> OpenForOffers(int id,
        CargoFlowDbContext db, IReferenceData referenceData)
    {
        var shipment = await db.Shipments.FirstOrDefaultAsync(s => s.Id == id);
        if (shipment is null)
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Shipment Not Found",
                detail: $"shipment #{id} does not exists");
        }

        var openForOffersStatusId = referenceData.GetShipmentStatusId(ShipmentStatusName.OpenForOffers);
        var draftStatusId = referenceData.GetShipmentStatusId(ShipmentStatusName.Draft);
        
        if (shipment.ShipmentStatusId == openForOffersStatusId)
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Shipment Status Conflict",
                detail: $"Shipment #{id} is already opened for offers");           
        }
        
        if (shipment.ShipmentStatusId != draftStatusId)
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Shipment Status Conflict",
                detail: $"Shipment #{id} is not Draft, only draft shipments can be opened for offers");
        }
        
        shipment.ShipmentStatusId = openForOffersStatusId;

        var shipmentStatusHistory = new ShipmentStatusHistory()
        {
            ShipmentId = shipment.Id,
            ShipmentStatusId = openForOffersStatusId,
            DateTime = DateTime.UtcNow
        };
        
        db.ShipmentStatusHistories.Add(shipmentStatusHistory);

        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Concurrency Conflict",
                detail: $"Shipment #{id} was modified or opened for offers by another request");
        }

        return TypedResults.NoContent();

    }

    private static async Task<Results<NoContent, ProblemHttpResult>> CancelShipment(int id, CargoFlowDbContext db, IReferenceData referenceData)
    {
        var shipment = await db.Shipments.FirstOrDefaultAsync(s => s.Id == id);
        if (shipment is null)
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Shipment Not Found",
                detail: $"Shipment #{id} does not exist");
        }

        var cancelledStatusId = referenceData.GetShipmentStatusId(ShipmentStatusName.Cancelled);
        
        if (shipment.ShipmentStatusId == cancelledStatusId)
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Shipment Status Conflict",
                detail: $"Shipment #{id} already cancelled");
        }
        
        var openStatusId = referenceData.GetShipmentStatusId(ShipmentStatusName.OpenForOffers);
        var draftStatusId = referenceData.GetShipmentStatusId(ShipmentStatusName.Draft);
        var receivedStatusId = referenceData.GetShipmentStatusId(ShipmentStatusName.OffersReceived);

        if (!new HashSet<int>{openStatusId, draftStatusId, receivedStatusId}.Contains(shipment.ShipmentStatusId))
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Shipment Status Conflict",
                detail: $"Shipment #{id} has an invalid status, only 'Draft', 'Open for Offers' or 'Offers Received' shipment can be cancelled");
        }

        shipment.ShipmentStatusId = cancelledStatusId;

        var shipmentStatusHistory = new ShipmentStatusHistory()
        {
            ShipmentStatusId = cancelledStatusId,
            ShipmentId = shipment.Id,
            DateTime = DateTime.UtcNow
        };

        db.ShipmentStatusHistories.Add(shipmentStatusHistory);
        try
        {
            await db.SaveChangesAsync();
            return TypedResults.NoContent();
        }
        catch (DbUpdateConcurrencyException)
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Update Concurrency Conflict",
                detail: $"Shipment {id} has been modified by another concurrent request");
        }

    }

    public static async Task<Results<NoContent, ProblemHttpResult>> StartShipment(int id, CargoFlowDbContext db, IReferenceData referenceData)
    {
        var shipment = await db.Shipments.FirstOrDefaultAsync(s => s.Id == id);
        if (shipment is null)
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "No Shipment Found",
                detail: $"Shipment #{id} does not exist");
        }

        var awardedStatusId = referenceData.GetShipmentStatusId(ShipmentStatusName.Awarded);

        if (shipment.ShipmentStatusId != awardedStatusId)
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Shipment Status Conflict",
                detail: $"Shipment #{id} has invalid status, only 'Awarded' shipments can be started");
        }

        var inTransitStatusId = referenceData.GetShipmentStatusId(ShipmentStatusName.InTransit);

        shipment.ShipmentStatusId = inTransitStatusId;
        var shipmentStatusHistory = new ShipmentStatusHistory()
        {
            ShipmentId = shipment.Id,
            ShipmentStatusId = inTransitStatusId,
            DateTime = DateTime.UtcNow
        };
        db.ShipmentStatusHistories.Add(shipmentStatusHistory);
        
        try
        {
            await db.SaveChangesAsync();
            return TypedResults.NoContent();
        }
        catch (DbUpdateConcurrencyException)
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Update Concurrency Exception",
                detail: $"Shipment #{id} was modified or started by another concurrent request");
        }
    }

    public static async Task<Results<NoContent, ProblemHttpResult>> DeliverShipment(
        int id, 
        CargoFlowDbContext db, 
        IReferenceData referenceData)
    {
        var shipment = await db.Shipments.FirstOrDefaultAsync(s => s.Id == id);
        if (shipment is null)
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Shipment Not Found",
                detail: $"Shipment #{id} does not exists");
        }

        var inTransitStatusId = referenceData.GetShipmentStatusId(ShipmentStatusName.InTransit);
        if (shipment.ShipmentStatusId != inTransitStatusId)
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Shipment Status Conflict",
                detail: $"Shipment #{id} has invalid status, only 'InTransit' shipments can be delivered");
        }

        var deliveredStatusId = referenceData.GetShipmentStatusId(ShipmentStatusName.Delivered);

        shipment.ShipmentStatusId = deliveredStatusId;

        var shipmentStatusHistory = new ShipmentStatusHistory()
        {
            ShipmentId = shipment.Id,
            ShipmentStatusId = deliveredStatusId,
            DateTime = DateTime.UtcNow
        };

        db.ShipmentStatusHistories.Add(shipmentStatusHistory);
        
        try
        {
            await db.SaveChangesAsync();
            return TypedResults.NoContent();
        }
        catch (DbUpdateConcurrencyException)
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Update Concurrency Conflict",
                detail: $"Shipment {id} was modified or delivered by another concurrent request");
        }
    }
}
