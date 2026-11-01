using CargoFlow.Data;
using CargoFlow.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CargoFlow.Helpers;

public interface IReferenceData
{
    public int GetShipmentStatusId(ShipmentStatusName shipmentStatusName);
    public int GetOfferStateId(OfferStateName offerStateName);
}

public class ReferenceData : IReferenceData
{
    private readonly Dictionary<ShipmentStatusName, int> _shipmentStatuses;
    private readonly Dictionary<OfferStateName, int> _offerStates;
    
    public ReferenceData(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CargoFlowDbContext>();

        _shipmentStatuses = db.ShipmentStatuses
            .AsNoTracking()
            .ToDictionary(ss => ss.Name, ss => ss.Id);

        _offerStates = db.OfferStates
            .AsNoTracking()
            .ToDictionary(os => os.Name, os => os.Id);
    }

    public int GetShipmentStatusId(ShipmentStatusName shipmentStatusName)
    {
        return _shipmentStatuses.TryGetValue(shipmentStatusName, out var id) switch
        {
            true => id,
            false => throw new InvalidOperationException($"Shipment status {shipmentStatusName} was not found in the database. Did you run the seeder?")
        };
    }

    public int GetOfferStateId(OfferStateName offerStateName)
    {
        return _offerStates.TryGetValue(offerStateName, out var id) switch
        {
            true => id,
            false => throw new InvalidOperationException(
                $"Offer state {offerStateName} was not found in the database. Did you run the seeder?")
        };
    }
}