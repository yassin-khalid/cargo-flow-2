using System.Security.Claims;
using CargoFlow.Data;
using CargoFlow.Domain;
using CargoFlow.Dtos;
using CargoFlow.Helpers;
using CargoFlow.Services;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CargoFlow.Endpoints;

public static class ShipmentDocumentEndpoints
{
    public static RouteGroupBuilder MapShipmentDocumentEndpoint(this RouteGroupBuilder group)
    {
        group
            .MapPost("/{id:int}/documents", UploadMultipleDocuments)
            .DisableAntiforgery();
        group.MapGet("/{id:int}/documents", GetShipmentDocuments);
        group.MapGet("/{id:int}/documents/{documentId:int}", GetShipmentDocument);
        group.MapDelete("{id:int}/documents/{documentId:int}", DeleteShipmentDocument);
        return group;
    }

    private static async Task<Results<Ok<List<ShipmentDocument>>, ProblemHttpResult>> UploadMultipleDocuments(
        int id,
        CargoFlowDbContext db,
        [FromForm] IFormFileCollection files,
        ClaimsPrincipal user,
        IRecipientsReferenceData recipientsReferenceData,
        IFileStorageService fileStorageService,
        CancellationToken cancellationToken = default
    )
    {
        var recipientId = recipientsReferenceData.GetRecipientId(user);

        if (recipientId is not int uploaderId)
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status401Unauthorized,
                title: "Unauthorized",
                detail: "Couldn't find customer associated with this user"
            );
        }

        var shipment = await db.Shipments.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
        if (shipment == null)
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Shipment not found",
                detail: $"Shipment #{id} not found"
            );
        }

        if (shipment.CustomerId != uploaderId)
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status401Unauthorized,
                title: "Unauthorized Access",
                detail: $"You don't authorized to access shipment #{id}"
            );
        }

        var documentTasks = files
            .Where(file => file.Length > 0)
            .Select(file => fileStorageService
                .UploadShipmentDocumentAsync(file, id, uploaderId, "ShipmentDocuments")
            );

        var shipmentDocuments = await Task.WhenAll(documentTasks);

        db.ShipmentDocuments.AddRange(shipmentDocuments);
        await db.SaveChangesAsync(cancellationToken);
        return TypedResults.Ok(shipmentDocuments.ToList());
    }

    private static async Task<Results<Ok<List<ShipmentDocumentDto>>, ProblemHttpResult>> GetShipmentDocuments(
        int id,
        CargoFlowDbContext db,
        ClaimsPrincipal user,
        IRecipientsReferenceData recipientsReferenceData,
        CancellationToken cancellationToken = default
    )
    {
        var customerId = recipientsReferenceData.GetRecipientId(user);

        if (customerId is not int uploaderId)
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status401Unauthorized,
                title: "Unauthorized",
                detail: "Couldn't find customer associated with this user"
            );
        }

        var shipment = await db.Shipments
            .Include(s => s.Documents)
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

        if (shipment is null)
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Shipment not found",
                detail: $"Shipment #{id} does not exist"
            );
        }

        if (shipment.CustomerId != uploaderId)
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status401Unauthorized,
                title: "Unauthorized Access",
                detail: $"You're not authorized to access shipment #{id}"
            );
        }


        return TypedResults.Ok<List<ShipmentDocumentDto>>([..shipment.Documents
            .Select(d => new ShipmentDocumentDto(
                d.Id,
                d.ShipmentId,
                d.OriginalFileName,
                d.StorageKey,
                d.ContentType,
                d.FileSize,
                d.UploadedById,
                d.UploadedAt)
            )]
        );
    }

    private static async Task<Results<FileStreamHttpResult, ProblemHttpResult>> GetShipmentDocument(
        int id,
        int documentId,
        CargoFlowDbContext db,
        ClaimsPrincipal user,
        IRecipientsReferenceData recipientsReferenceData,
        IFileStorageService fileStorageService,
        CancellationToken cancellationToken = default
    )
    {
        var customerId = recipientsReferenceData.GetRecipientId(user);

        if (customerId is not int uploaderId)
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status401Unauthorized,
                title: "Unauthorized",
                detail: "Couldn't find customer associated with this user"
            );
        }
        var shipment = await db.Shipments
            .Include(s => s.Documents)
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

        if (shipment is null)
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Shipment not found",
                detail: $"Shipment #{id} does not exist"
            );
        }

        var shipmentDocument = shipment.Documents
            .FirstOrDefault(d => d.Id == documentId);

        if (shipmentDocument == null)
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Document not found",
                detail: $"Shipment #{id} does not have a document #{documentId}"
            );
        }

        var storedFile = await fileStorageService
            .OpenShipmentDocumentAsync(shipmentDocument, "ShipmentDocuments");

        return TypedResults.File(
            storedFile.Stream,
            storedFile.ContentType,
            storedFile.FileDownloadName,
            enableRangeProcessing: true
        );

    }

    private static async Task<Results<NoContent, ProblemHttpResult>> DeleteShipmentDocument(
        int id,
        int documentId,
        CargoFlowDbContext db,
        ClaimsPrincipal user,
        IRecipientsReferenceData recipientsReferenceData,
        IFileStorageService fileStorageService,
        CancellationToken cancellationToken = default
    )
    {
        var customerId = recipientsReferenceData.GetRecipientId(user);
        if (customerId is not int uploaderId)
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status401Unauthorized,
                title: "Unauthorized",
                detail: "Couldn't find customer associated with this user"
            );
        }

        var shipment = await db.Shipments
            .Include(s => s.Documents)
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

        if (shipment is null)
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Shipment not found",
                detail: $"Shipment #{id} does not exist"
            );
        }

        var document = shipment.Documents.FirstOrDefault(d => d.Id == documentId);
        if (document is null)
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Document not found",
                detail: $"Shipment #{id} does not have a document #{documentId}"
            );
        }

        try
        {
            fileStorageService.DeleteShipmentDocument(document, "ShipmentDocuments");
            db.ShipmentDocuments.Remove(document);
            await db.SaveChangesAsync(cancellationToken);

            return TypedResults.NoContent();
        }
        catch (Exception ex)
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Unable to delete file",
                detail: ex.Message
            );
        }


    }
}
