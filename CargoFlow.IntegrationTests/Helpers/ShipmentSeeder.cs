using CargoFlow.Data;
using CargoFlow.Domain;
using Microsoft.EntityFrameworkCore;

namespace CargoFlow.IntegrationTests.Helpers;

public record OfferSpec(string CompanyName, decimal Price, string StateName, bool HasWon = false);
public static class ShipmentSeeder
{
    private static (Address pickup, Address destination) GenerateUniqueAddresses()
    {
        var hash = Guid.NewGuid().GetHashCode();
        // Base coordinates (e.g., starting around Saudi Arabia / Red Sea region)
        // We add a tiny offset derived from the hash (e.g., +/- up to 5 degrees)
        var pickupLat = 21.4858M + ((hash % 1000) * 0.001M);
        var pickupLng = 39.1925M + (((hash / 1000) % 1000) * 0.001M);

        var destLat = 24.0895M + (((hash / 1000000) % 1000) * 0.001M);
        var destLng = 38.0618M + (((hash / 100000000) % 1000) * 0.001M);

        var pickup = new Address()
        {
            Latitude = pickupLat,
            Longitude = pickupLng
        };

        var destination = new Address()
        {
            Latitude = destLat,
            Longitude = destLng
        };

        return (pickup, destination);
    }
    
    
    public static async Task<(Shipment, List<Offer>)> SeedShipmentsWithOffersAsync(
        CargoFlowDbContext db, 
        string shipmentStatus, 
        string weightType, 
        decimal weight, 
        OfferSpec[] offerSpecs)
    {
        var targetStatus = await db.ShipmentStatuses.FirstAsync(ss => ss.Name == shipmentStatus);
        var targetType = await db.WeightTypes.FirstAsync(wt => wt.Name == weightType);
        
        var (pickup, destination) = GenerateUniqueAddresses();
        
        db.Addresses.AddRange(pickup, destination);

        var uniqueSuffix = Guid.NewGuid().ToString()[..8];

        var customer = new Customer()
        {
            Name = $"Cust-{uniqueSuffix}",
            NationalId = $"Nat-{uniqueSuffix[..6]}"
        };
        db.Customers.Add(customer);
        
        await db.SaveChangesAsync();

        var shipment = new Shipment()
        {
            DestinationId = destination.Id,
            PickupId = pickup.Id,
            Weight = weight,
            CustomerId = customer.Id,
            WeightTypeId = targetType.Id,
            ShipmentStatusId = targetStatus.Id,
        };

        db.Shipments.Add(shipment);
        await db.SaveChangesAsync();

        List<Offer> offers = [];
        for (var i = 0; i < offerSpecs.Length; i++)
        {
            var spec = offerSpecs[i];
            var subSuffix = $"{uniqueSuffix}-{i}";

            var company = new Company()
            {
                Name = $"{spec.CompanyName} {subSuffix}".Trim()[..Math.Min(40, $"{spec.CompanyName} {subSuffix}".Length)],
                TaxID = $"TAX-{subSuffix}".Trim()[..Math.Min(15, $"TAX-{subSuffix}".Length)],
                Email = $"test-{subSuffix}@company.com",
            };

            db.Companies.Add(company);
            await db.SaveChangesAsync();
            
            var offerState = await db.OfferStates.FirstAsync(os => os.Name == spec.StateName);
            var offer = new Offer()
            {
                CompanyId = company.Id,
                HasWon = spec.HasWon,
                OfferStateId = offerState.Id,
                Price = spec.Price,
                ShipmentId = shipment.Id,
                SubmittedDate = DateTime.UtcNow.AddMinutes(-i)
            };
            db.Offers.Add(offer);
            await db.SaveChangesAsync();
            offers.Add(offer);
        }

        return (shipment, offers);
    }
}