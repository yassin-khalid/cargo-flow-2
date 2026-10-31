using CargoFlow.Data;
using CargoFlow.Domain;
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

    private static async Task<Results<CreatedAtRoute<ShipmentDto>, ValidationProblem, ProblemHttpResult>> CreateShipment(CargoFlowDbContext db, CreateShipmentRequestDto dto)
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

        var draftShipmentId = await db
            .ShipmentStatuses
            .Where(s => s.Name == "Draft")
            .Select(s => s.Id)
            .FirstOrDefaultAsync();

        if (draftShipmentId is 0)
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Database Configuration Error",
                detail: "Required system lookup data ('Draft' status) is missing. Please ensure the database seeder has been executed.");
        }

        var shipment = new Shipment()
        {
            CustomerId = dto.CustomerId,
            DestinationId = dto.DestinationId,
            PickupId = dto.PickupId,
            WeightTypeId = dto.WeightTypeId,
            ShipmentStatusId = draftShipmentId,
            Weight = dto.Weight
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
        CargoFlowDbContext db)
    {
        var shipment = await db.Shipments.FirstOrDefaultAsync(s => s.Id == id);
        if (shipment is null)
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Shipment Not Found",
                detail: $"shipment #{id} does not exists");
        }

        var draftStatusId = await db.ShipmentStatuses
            .Where(ss => ss.Name == "Draft")
            .Select(ss => ss.Id)
            .FirstOrDefaultAsync();

        if (draftStatusId == 0)
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Database Configuration Error",
                detail: "Required system lookup data ('Draft' status) is missing. Please ensure the database seeder has been executed.");           
        }
        
        var openForOffersStatusId =
            await db.ShipmentStatuses
                .Where(ss => ss.Name == "Open for Offers")
                .Select(ss => ss.Id)
                .FirstOrDefaultAsync();

        if (openForOffersStatusId == 0)
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Database Configuration Error",
                detail: "Required system lookup data ('Open For Offers' status) is missing. Please ensure the database seeder has been executed.");
        }

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

    private static async Task<Results<NoContent, ProblemHttpResult>> CancelShipment(int id, CargoFlowDbContext db)
    {
        var shipment = await db.Shipments.FirstOrDefaultAsync(s => s.Id == id);
        if (shipment is null)
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Shipment Not Found",
                detail: $"Shipment #{id} does not exist");
        }

        var cancelledStatusId = await db.ShipmentStatuses
            .Where(ss => ss.Name == "Cancelled")
            .Select(ss => ss.Id)
            .FirstOrDefaultAsync();

        if (cancelledStatusId == 0)
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Database Configuration Error",
                detail: "Required system lookup data ('Cancelled' status) is missing. Please ensure the database seeder has been executed.");
        }

        if (shipment.ShipmentStatusId == cancelledStatusId)
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Shipment Status Conflict",
                detail: $"Shipment #{id} already cancelled");
        }
        
        var draftStatusId = await db.ShipmentStatuses
            .Where(ss => ss.Name == "Draft")
            .Select(ss => ss.Id)
            .FirstOrDefaultAsync();

        if (draftStatusId == 0)
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Database Configuration Error",
                detail: "Required system lookup data ('Draft' status) is missing. Please ensure the database seeder has been executed.");
        }
        
        
        var receivedStatusId = await db.ShipmentStatuses
            .Where(ss => ss.Name == "Offers Received")
            .Select(ss => ss.Id)
            .FirstOrDefaultAsync();

        if (receivedStatusId == 0)
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Database Configuration Error",
                detail: "Required system lookup data ('Offers Received' status) is missing. Please ensure the database seeder has been executed.");
        }
        
        var openStatusId = await db.ShipmentStatuses
            .Where(ss => ss.Name == "Open for Offers")
            .Select(ss => ss.Id)
            .FirstOrDefaultAsync();

        if (openStatusId == 0)
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Database Configuration Error",
                detail: "Required system lookup data ('Open for Offers' status) is missing. Please ensure the database seeder has been executed.");
        }

        if (!new HashSet<int>{openStatusId, draftStatusId, receivedStatusId}.Contains(shipment.ShipmentStatusId))
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Shipment Status Conflict",
                detail: $"Shipment #{id} has an invalid status, only 'Draft', 'Open for Offers' or 'Offers Received' shipment can be cancelled");
        }

        shipment.ShipmentStatusId = cancelledStatusId;
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
}
