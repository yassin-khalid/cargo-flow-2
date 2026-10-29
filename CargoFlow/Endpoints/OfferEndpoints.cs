using CargoFlow.Data;
using CargoFlow.Domain;
using CargoFlow.Dtos;
using CargoFlow.Helpers;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CargoFlow.Endpoints;

public static class OfferEndpoints
{
    public static RouteGroupBuilder MapOfferEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/{shipmentId:int}/offers", SubmitOffer);
        group.MapPost("/{shipmentId:int}/offers/{offerId:int}/award", AwardOffer);
        group.MapPost("/{shipmentId:int}/offers/{offerId:int}/withdraw", WithdrawOffer);
        group.MapGet("/{shipmentId:int}/offers/{id:int}", GetShipmentOffer)
            .WithName(EndpointRouteNames.GetShipmentOfferByIdRoute);
        group.MapGet("/{shipmentId:int}/offers", GetShipmentOffers);
        return group;
    }

    private static async Task<Results<Ok<List<OfferDto>>, ProblemHttpResult>> GetShipmentOffers(int shipmentId,
        CargoFlowDbContext db)
    {
        var isShipmentExists = await db.Shipments.AnyAsync(s => s.Id == shipmentId);
        if (!isShipmentExists)
        {
            return HttpResultProblems.NotFoundProblem("shipment_id", shipmentId);
        }

        return TypedResults.Ok(await db.Offers
            .Where(o => o.ShipmentId == shipmentId)
            .OrderByDescending(o => o.SubmittedDate)
            .Select(o =>
                new OfferDto(o.Id, o.ShipmentId, o.CompanyId, o.SubmittedDate, o.Price, o.OfferStateId, o.HasWon))
            .ToListAsync());
    }

    private static async Task<Results<Ok<OfferDto>, ProblemHttpResult>> GetShipmentOffer(int id, int shipmentId, CargoFlowDbContext db)
    {
        var isShipmentExists = await db.Shipments.AnyAsync(s => s.Id == shipmentId);
        if (!isShipmentExists)
        {
            return HttpResultProblems.NotFoundProblem("shipment_id", shipmentId);
        }

        return await db.Offers
                .Where(o => o.Id == id && o.ShipmentId == shipmentId)
                .Select(o => new OfferDto(o.Id, o.ShipmentId, o.CompanyId, o.SubmittedDate, o.Price, o.OfferStateId,
                    o.HasWon))
                .FirstOrDefaultAsync() switch
            {
                { } offer => TypedResults.Ok(offer),
                _ => HttpResultProblems.NotFoundProblem("offer_id", id)
            };
    }
    private static async Task<Results<CreatedAtRoute<OfferDto>, NotFound, ValidationProblem, ProblemHttpResult>> SubmitOffer(
       int shipmentId,
       SubmitOfferRequestBody body,
       CargoFlowDbContext db)
    {
        var shipment = await db.Shipments.FirstOrDefaultAsync(s => s.Id == shipmentId);
        if (shipment is null)
        {
            return HttpResultProblems.NotFoundProblem("shipment_id", shipmentId);
        }

        var isCompanyExists = await db.Companies.AnyAsync(c => c.Id == body.CompanyId);
        if (!isCompanyExists)
        {
            return HttpResultProblems.NotFoundProblem("company_id", body.CompanyId);
        }

        var isNotTheFirstOffer =
           await db.Offers.AnyAsync(o => o.CompanyId == body.CompanyId && o.ShipmentId == shipmentId);
        if (isNotTheFirstOffer)
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Duplicate offer",
                detail: $"you've already a submitted an offer for this shipment #{shipmentId}");
        }

        var openForOffersStatusId = await db.ShipmentStatuses
            .Where(ss => ss.Name == "Open for Offers")
            .Select(ss => ss.Id)
            .FirstOrDefaultAsync();
        
        if (openForOffersStatusId == 0)
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Database Configuration Error",
                detail: "Required system lookup data ('Open for Offers' status) is missing. Please ensure the database seeder has been executed.");
        }
        
        var offerReceivedStatus = await db.ShipmentStatuses
            .Where(ss => ss.Name == "Offers Received")
            .Select(ss => ss.Id)
            .FirstOrDefaultAsync();

        if (offerReceivedStatus == 0)
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Database Configuration Error",
                detail: "Required system lookup data ('Offers Received' status) is missing. Please ensure the database seeder has been executed.");
        }
        
        if (shipment.ShipmentStatusId != offerReceivedStatus && shipment.ShipmentStatusId != openForOffersStatusId)
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Shipment Status Conflict Error",
                detail: $"shipment with id {shipmentId} does not have 'Open for Offers' or 'Offers Received' status");
        }

        var offerStateId = await db.OfferStates
            .Where(os => os.Name == "Submitted")
            .Select(os => os.Id)
            .FirstOrDefaultAsync();

        if (offerStateId == 0)
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status500InternalServerError, 
                title: "Database Configuration Error", 
                detail: "Required system lookup data ('Submitted' state) is missing. Please ensure the database seeder has been executed.");
        }

        var offer = new Offer()
        {
            CompanyId = body.CompanyId,
            ShipmentId = shipmentId,
            OfferStateId = offerStateId,
            Price = body.Price,
            SubmittedDate = DateTime.UtcNow,
            HasWon = false
        };

        db.Offers.Add(offer);

        if (shipment.ShipmentStatusId == openForOffersStatusId)
        {
            shipment.ShipmentStatusId = offerReceivedStatus;
        }

        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (ExceptionsHelpers.IsUniqueConstraintViolation(ex,
                                               "IX_Offers_ShipmentId_CompanyId"))
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Duplicate offer",
                detail: $"you've already a submitted an offer for this shipment #{shipmentId}");
        }
        catch (DbUpdateException ex) when (ExceptionsHelpers.IsForeignKeyViolation(ex,
                                               "FK_Offers_OfferStates_OfferStateId"))
        {

            return HttpResultProblems.ValidationProblem("offer_state_id", ["offer_state_id not exists"]);
        }

        catch (DbUpdateException ex) when (ExceptionsHelpers.IsForeignKeyViolation(ex, "FK_Offers_Companies_CompanyId"))
        {
            return HttpResultProblems.ValidationProblem("company_id", ["company_id not exists"]);
        }

        catch (DbUpdateException ex) when
            (ExceptionsHelpers.IsForeignKeyViolation(ex, "FK_Offers_Shipments_ShipmentId"))
        {
            return HttpResultProblems.ValidationProblem("shipment_id", ["shipment_id not exists"]);
        }
        catch (DbUpdateConcurrencyException)
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Concurrency Conflict",
                detail: $"Shipment #{shipmentId} was modified by another request");
        }

        return TypedResults.CreatedAtRoute(
            routeName: EndpointRouteNames.GetShipmentOfferByIdRoute,
            value: new OfferDto(
                offer.Id, 
                offer.ShipmentId, 
                offer.CompanyId, 
                offer.SubmittedDate, 
                offer.Price,
                offer.OfferStateId, 
                offer.HasWon), 
            routeValues:  new
            {
                id = offer.Id, 
                shipmentId
            });
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> AwardOffer(int shipmentId, int offerId, CargoFlowDbContext db)
    {
        var shipment = await db.Shipments.FirstOrDefaultAsync(s => s.Id == shipmentId);
        if (shipment is null)
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Shipment not found",
                detail: $"shipment #{shipmentId} not found"
            );
        }

        var offer = await db.Offers
            .FirstOrDefaultAsync(o => o.Id == offerId && o.ShipmentId == shipmentId);
        if (offer is null)
        {
            
            return TypedResults.Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Offer not found",
                detail: $"Offer #{offerId} either does not exists or does not belong to shipment #{shipmentId}"
            );
        }

        var validShipmentStatusIds = await db.ShipmentStatuses
            .Where(ss => ss.Name == "Open for Offers" || ss.Name == "Offers Received")
            .Select(ss => ss.Id)
            .ToListAsync();

        if (validShipmentStatusIds.Count < 2)
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Database Configuration Error",
                detail: "Required system lookup data ('Open For Offers' or 'Offers Received' status) is missing. Please ensure the database seeder has been executed.");
        }

        if (!validShipmentStatusIds.Contains(shipment.ShipmentStatusId))
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Shipment Status Issue",
                detail: $"Shipment #{shipmentId} is not open for offers"
            );
        }

        var offerStateId = await db.OfferStates
            .Where(os => os.Name == "Accepted")
            .Select(os => os.Id)
            .FirstOrDefaultAsync();

        if (offerStateId == 0)
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Database Configuration Error",
                detail: "Required system lookup data ('Accepted' state) is missing. Please ensure the database seeder has been executed."
            );
        }

        var shipmentStatusId = await db.ShipmentStatuses
            .Where(ss => ss.Name == "Awarded")
            .Select(ss => ss.Id)
            .FirstOrDefaultAsync();

        if (shipmentStatusId == 0)
        {
            
            return TypedResults.Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Database Configuration Error",
                detail: "Required system lookup data ('Awarded' status) is missing. Please ensure the database seeder has been executed."
            );
        }


        var rejectedOfferStateId = await db.OfferStates
            .Where(os => os.Name == "Rejected")
            .Select(os => os.Id)
            .FirstOrDefaultAsync();

        if (rejectedOfferStateId is 0)
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Database Configuration Error",
                detail: "Required system lookup data ('Rejected' state) is missing. Please ensure the database seeder has been executed."
            );
        }

        var otherOffers = await db.Offers
            .Where(o => o.Id != offerId && o.ShipmentId == shipmentId)
            .ToListAsync();

        foreach (var oo in otherOffers)
        {
            oo.OfferStateId = rejectedOfferStateId;
        }
            
        offer.OfferStateId = offerStateId;
        offer.HasWon = true;
        shipment.ShipmentStatusId = shipmentStatusId;

        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (ExceptionsHelpers
                                               .IsUniqueConstraintViolation(ex, "IX_Offers_ShipmentId"))
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Invalid Operation",
                detail: $"shipment #{shipmentId} already has an accepted offer, can not accept offer #{offerId}");
        }
        catch (DbUpdateConcurrencyException)
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Concurrency Conflict",
                detail: $"Shipment #{shipmentId} was modified or awarded by another request");
        }

        return TypedResults.NoContent();
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> WithdrawOffer(int shipmentId, int offerId,
        CargoFlowDbContext db)
    {
        var shipment = await db.Shipments.FirstOrDefaultAsync(s => s.Id == shipmentId);
        if (shipment is null)
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Shipment Not Found",
                detail: $"shipment #{shipmentId} does not exist");
        }

        var offer = await db.Offers.FirstOrDefaultAsync(o => o.Id == offerId && o.ShipmentId == shipmentId);
        if (offer is null)
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Offer Not Found",
                detail: $"offer #{offerId} is either does not exist or does not belong to shipment #{shipmentId}");
        }

        var openForOffersStatusId = await db.ShipmentStatuses
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

        var offersReceivedStatusId = await db.ShipmentStatuses
            .Where(ss => ss.Name == "Offers Received")
            .Select(ss => ss.Id)
            .FirstOrDefaultAsync();
        
        if (offersReceivedStatusId == 0)
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Database Configuration Error",
                detail: "Required system lookup data ('Offers Received' status) is missing. Please ensure the database seeder has been executed.");
        }

        if (shipment.ShipmentStatusId != offersReceivedStatusId
            && shipment.ShipmentStatusId != openForOffersStatusId)
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Shipment Status Conflict Error",
                detail: $"shipment #{shipmentId} does not have status 'Offers Received' or 'Open for Offers'");
        }

        var submittedStateId = await db.OfferStates
            .Where(os => os.Name == "Submitted")
            .Select(os => os.Id)
            .FirstOrDefaultAsync();

        if (submittedStateId == 0)
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Database Configuration Error",
                detail: "Required system lookup data ('Submitted' state) is missing. Please ensure the database seeder has been executed.");           
        }

        if (offer.OfferStateId != submittedStateId)
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Offer State Conflict Error",
                detail: $"offer #{offerId} does not have 'Submitted' state");           
        }

        var withdrawnStateId = await db.OfferStates
            .Where(os => os.Name == "Withdrawn")
            .Select(os => os.Id)
            .FirstOrDefaultAsync();

        if (withdrawnStateId == 0)
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Database Configuration Error",
                detail: "Required system lookup data ('Withdrawn' state) is missing. Please ensure the database seeder has been executed.");           
        }

        offer.OfferStateId = withdrawnStateId;

        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Concurrency Conflict",
                detail: $"offer #{offerId} was modified or awarded by another process concurrently. Please reload and try again.");
        }

        return TypedResults.NoContent();
    }
    
}
