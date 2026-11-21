using Bogus;
using CargoFlow.Domain;

namespace CargoFlow.IntegrationTests.Helpers;

public static class Fakers
{
    private const int DefaultSeed = 1234;

    public static Faker<Address> Address(int seed = DefaultSeed) => new Faker<Address>()
        .UseSeed(seed)
        .RuleFor(a => a.Latitude, f => Math.Round(f.Random.Decimal(17, 31), 6))
        .RuleFor(a => a.Longitude, f => Math.Round(f.Random.Decimal(36, 54), 6));

    public static Faker<Driver> Driver(int companyId, int seed = DefaultSeed) => new Faker<Driver>()
        .UseSeed(seed)
        .RuleFor(d => d.CompanyId, companyId)
        .RuleFor(d => d.DateOfBirth, f => DateOnly.FromDateTime(f.Date.Past(25, DateTime.Today.AddYears(-22))))
        .RuleFor(d => d.Phone, f => f.Phone.PhoneNumber("05########"))
        .RuleFor(d => d.Name, f => Trunc(f.Name.FullName(), 25));

    public static Faker<Vehicle> Vehicle(int companyId, int weightTypeId, int seed = DefaultSeed) => new Faker<Vehicle>()
        .UseSeed(seed)
        .RuleFor(v => v.CompanyId, companyId)
        .RuleFor(v => v.InspectionId, f => f.Random.ReplaceNumbers("INS-########"))
        .RuleFor(v => v.InsuranceId, f => f.Random.ReplaceNumbers("POL-########"))
        .RuleFor(v => v.InspectionIdExpiryDate, f => DateOnly.FromDateTime(f.Date.Soon(356)))
        .RuleFor(v => v.InsuranceIdExpiryDate, f => DateOnly.FromDateTime(f.Date.Soon(356)))
        .RuleFor(v => v.MaxWeight, f => f.Random.Decimal(1000, 30000))
        .RuleFor(v => v.IsAvailable, f => f.Random.Bool(0.7f))
        .RuleFor(v => v.WeightTypeId, weightTypeId);

    public static Faker<Shipment> Shipment(int customerId, int statusId, int weightTypeId, int seed = DefaultSeed) => new Faker<Shipment>()
        .UseSeed(seed)
        .RuleFor(s => s.CustomerId, customerId)
        .RuleFor(s => s.Destination, _ => Address(seed).Generate())
        .RuleFor(s => s.Pickup, _ => Address(seed + 1).Generate())
        .RuleFor(s => s.ShipmentStatusId, statusId)
        .RuleFor(s => s.Weight, f => Math.Round(f.Random.Decimal(1, 20000), 2))
        .RuleFor(s => s.WeightTypeId, weightTypeId);

    public static Faker<Offer> Offer(int companyId, int shipmentId, int offerStateId, bool hasWon = false, int seed = DefaultSeed) => new Faker<Offer>()
        .UseSeed(seed)
        .RuleFor(o => o.CompanyId, companyId)
        .RuleFor(o => o.OfferStateId, offerStateId)
        .RuleFor(o => o.SubmittedDate, f => f.Date.Recent(5).ToUniversalTime())
        .RuleFor(o => o.ExpirationDate, (f, o) => o.SubmittedDate.AddDays(f.Random.Int(1, 30)))
        .RuleFor(o => o.Price, f => Math.Round(f.Random.Decimal(200, 15000), 2))
        .RuleFor(o => o.ShipmentId, shipmentId)
        .RuleFor(o => o.HasWon, hasWon);

    private static string Trunc(string s, int max) => s.Length <= max ? s : s[..max];

}
