using CargoFlow.Data;
using CargoFlow.Domain;
using CargoFlow.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CargoFlow.IntegrationTests.Helpers;


public record OfferExpirationTest(
    string CompanyName,
    decimal Price,
    DateTime SubmittedDate,
    DateTime ExpirationDate,
    OfferStateName StateName = OfferStateName.Submitted,
    bool HasWon = false);

public static class OfferSeeder
{
    public static async Task<(Shipment, List<Offer>)> SeedOffersWithSumittedAndExpirationDate(CargoFlowDbContext db, OfferExpirationTest[] toBeExpiredOffers)
    {
        var targetStatus = await db.ShipmentStatuses.FirstAsync(ss => ss.Name == ShipmentStatusName.OffersReceived);
        var targetType = await db.WeightTypes.FirstAsync(wt => wt.Name == "Ton (t)");

        var (pickup, destination) = UniqueAddressSeeder.GenerateUniqueAddresses();

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
            Weight = 100,
            CustomerId = customer.Id,
            WeightTypeId = targetType.Id,
            ShipmentStatusId = targetStatus.Id,
        };

        db.Shipments.Add(shipment);
        await db.SaveChangesAsync();

        List<Offer> offers = [];
        for (var i = 0; i < toBeExpiredOffers.Length; i++)
        {
            var spec = toBeExpiredOffers[i];
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
                SubmittedDate = spec.SubmittedDate,
                ExpirationDate = spec.ExpirationDate
            };
            db.Offers.Add(offer);
            await db.SaveChangesAsync();
            offers.Add(offer);
        }

        return (shipment, offers);
    }
}
