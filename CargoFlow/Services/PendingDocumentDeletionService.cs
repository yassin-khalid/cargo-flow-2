using CargoFlow.Data;
using CargoFlow.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CargoFlow.Services;

public interface IPendingDocumentDeletionService
{
    Task DeletePendingDocuments(CancellationToken cancellationToken);
}

public class PendingDocmentDeletionService(
    CargoFlowDbContext db,
    ILogger<PendingDocmentDeletionService> logger,
    IFileStorageService fileStorageService
) : IPendingDocumentDeletionService
{
    public async Task DeletePendingDocuments(CancellationToken cancellationToken)
    {
        var pendingDocuments = await db.ShipmentDocuments
            .Where(d => d.DeletionStatus == DocumentDeletionStatus.PendingDeletion)
            .ToListAsync(cancellationToken);

        foreach (var document in pendingDocuments)
        {
            try
            {
                fileStorageService.DeleteShipmentDocument(document);
                db.ShipmentDocuments.Remove(document);
                await db.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                db.Entry(document).State = EntityState.Unchanged;
                logger.LogWarning(ex, "Failed to finalize deletion for ShipmentDocument #{DocumentId}. Will retry", document.Id);
            }
        }
    }
}
