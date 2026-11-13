using CargoFlow.Data;
using CargoFlow.Domain.Enums;
using CargoFlow.Helpers;
using Microsoft.EntityFrameworkCore;

namespace CargoFlow.Workers;

public interface IOfferExpirationService
{
    Task CheckAndMarkExpirationOfferAsync(CancellationToken stoppingToken);
}
public class OfferExpirationService(ILogger<OfferExpirationService> logger, CargoFlowDbContext db, IReferenceData referenceData) : IOfferExpirationService
{
    public async Task CheckAndMarkExpirationOfferAsync(CancellationToken stoppingToken)
    {
        var now = DateTime.UtcNow;
        var expiredStateId = referenceData.GetOfferStateId(OfferStateName.Expired);
        var submittedStateId = referenceData.GetOfferStateId(OfferStateName.Submitted);
        var updatedRows =
            await db.Offers
                .Where(o => o.ExpirationDate <= now && o.OfferStateId == submittedStateId)
                .ExecuteUpdateAsync(builder => builder
                    .SetProperty(o => o.OfferStateId, expiredStateId), stoppingToken);

        if (updatedRows > 0)
        {
            logger.LogInformation("Successfully bulk-updated {Count} expired offers at {Time}", updatedRows, now);
        }
    }
}
