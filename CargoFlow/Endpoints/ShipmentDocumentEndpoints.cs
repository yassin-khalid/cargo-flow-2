using System.Security.Claims;
using CargoFlow.Data;
using CargoFlow.Domain;
using CargoFlow.Domain.Enums;
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

    private static async Task<Results<Ok<List<ShipmentDocumentDto>>, ProblemHttpResult>> UploadMultipleDocuments(
        int id,
        CargoFlowDbContext db,
        [FromForm] IFormFileCollection files,
        ClaimsPrincipal user,
        IRecipientsReferenceData recipientsReferenceData,
        IFileStorageService fileStorageService,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken = default
    )
    {
        var logger = loggerFactory.CreateLogger(nameof(ShipmentDocumentEndpoints));
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
                statusCode: StatusCodes.Status403Forbidden,
                title: "Unauthorized Access",
                detail: $"You're not authorized to access shipment #{id}"
            );
        }

        if (files.Count == 0)
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "No file found",
                detail: "No shipment document file found"
            );
        }

        var isAnyEmpty = files.Any(f => f.Length == 0);
        if (isAnyEmpty)
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Empty file",
                detail: "Uploaded files contains an empty file"
            );
        }

        var validFiles = files.Where(f => f.Length > 0).ToList();
        var uploadedDocuments = new List<ShipmentDocument>(validFiles.Count);

        try
        {
            foreach (var file in validFiles)
            {
                var document = await fileStorageService.UploadShipmentDocumentAsync(
                    file,
                    id,
                    uploaderId,
                    cancellationToken
                );
                uploadedDocuments.Add(document);
            }
            db.ShipmentDocuments.AddRange(uploadedDocuments);
            await db.SaveChangesAsync(cancellationToken);
            return TypedResults.Ok(uploadedDocuments
                .Select(d => new ShipmentDocumentDto(
                    d.Id,
                    d.ShipmentId,
                    d.OriginalFileName,
                    d.ContentType,
                    d.FileSize,
                    d.UploadedById,
                    d.UploadedAt
                ))
                .ToList()
            );
        }
        catch (Exception ex)
        {
            foreach (var document in uploadedDocuments)
            {
                try
                {
                    fileStorageService.DeleteShipmentDocument(document);
                }
                catch (Exception cleanupException)
                {
                    logger.LogError(cleanupException, "Failed to cleanup uploaded document {DocumentId}", document.Id);
                }
            }
            return ex switch
            {
                InvalidOperationException or ArgumentException => TypedResults.Problem(
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "Bad Request",
                    detail: ex.Message
                ),
                _ => TypedResults.Problem(
                    statusCode: StatusCodes.Status500InternalServerError,
                    title: "Error while trying to upload shipment documents",
                    detail: "An unexpected error occured while saving shipment documents"
                )
            };
        }
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
                statusCode: StatusCodes.Status403Forbidden,
                title: "Unauthorized Access",
                detail: $"You're not authorized to access shipment #{id}"
            );
        }


        return TypedResults.Ok<List<ShipmentDocumentDto>>([..shipment.Documents
            .Where(d => d.DeletionStatus == DocumentDeletionStatus.Active) // Only active documents
            .Select(d => new ShipmentDocumentDto(
                d.Id,
                d.ShipmentId,
                d.OriginalFileName,
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

        if (shipment.CustomerId != uploaderId)
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status403Forbidden,
                title: "Unauthorized Access",
                detail: $"You're not authorized to access shipment #{id}"
            );
        }

        var shipmentDocument = shipment.Documents
            .FirstOrDefault(d => d.Id == documentId
                && d.DeletionStatus == DocumentDeletionStatus.Active
            );

        if (shipmentDocument == null)
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Document not found",
                detail: $"Shipment #{id} does not have a document #{documentId}"
            );
        }

        try
        {

            var storedFile = await fileStorageService
                .OpenShipmentDocumentAsync(shipmentDocument);

            return TypedResults.File(
                storedFile.Stream,
                storedFile.ContentType,
                storedFile.FileDownloadName,
                enableRangeProcessing: true
            );
        }
        catch (Exception ex) when (ex is FileNotFoundException)
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Unable to open file",
                detail: $"Couldn't open shipment document #{shipmentDocument.Id}"
            );
        }

    }

    private static async Task<Results<NoContent, ProblemHttpResult>> DeleteShipmentDocument(
        int id,
        int documentId,
        CargoFlowDbContext db,
        ClaimsPrincipal user,
        IRecipientsReferenceData recipientsReferenceData,
        IFileStorageService fileStorageService,
        TimeProvider timeProvider,
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
                statusCode: StatusCodes.Status403Forbidden,
                title: "Unauthorized Access",
                detail: $"You're not authorized to access shipment #{id}"
            );
        }

        var document = shipment.Documents
            .FirstOrDefault(d => d.Id == documentId
                && d.DeletionStatus == DocumentDeletionStatus.Active
            );

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
            document.DeletionStatus = DocumentDeletionStatus.PendingDeletion;
            document.DeletionRequestedAtUtc = timeProvider.GetUtcNow();
            await db.SaveChangesAsync(cancellationToken);
            return TypedResults.NoContent();
        }
        catch
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Unable to delete file",
                detail: "An unexpected error happened when trying to delete the document"
            );
        }


    }
}
