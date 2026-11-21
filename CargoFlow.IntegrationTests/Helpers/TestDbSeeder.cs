using CargoFlow.Data;
using CargoFlow.Domain;
using CargoFlow.Domain.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CargoFlow.IntegrationTests.Helpers;

public static class TestDbSeeder
{
    public static async Task SeedAsync(CargoFlowDbContext db, UserManager<ApplicationUser> userManager)
    {
        if (await userManager.FindByEmailAsync(TestData.CustomerEmail) is not null) return;

        // Lookups
        if (!await db.ShipmentStatuses.AnyAsync())
        {
            db.ShipmentStatuses.AddRange(Enum.GetValues<ShipmentStatusName>().Select(n => new ShipmentStatus() { Name = n }));
        }
        if (!await db.OfferStates.AnyAsync())
        {
            db.OfferStates.AddRange(Enum.GetValues<OfferStateName>().Select(o => new OfferState() { Name = o }));
        }
        if (!await db.WeightTypes.AnyAsync())
        {
            db.WeightTypes.AddRange(Enum.GetValues<WeightTypeName>().Select(w => new WeightType() { Name = w }));
        }
        await db.SaveChangesAsync();

        // Users
        var cu1 = await CreateUserAsync(userManager, TestData.CustomerEmail);
        var cu2 = await CreateUserAsync(userManager, TestData.OtherCustomerEmail);
        var co1 = await CreateUserAsync(userManager, TestData.CompanyEmail);
        var co2 = await CreateUserAsync(userManager, TestData.OtherCompanyEmail);

        var customer1 = new Customer()
        {
            UserId = cu1.Id,
            Name = "Test Customer",
            NationalId = "1000000001"
        };
        var customer2 = new Customer()
        {
            UserId = cu2.Id,
            Name = "Other Customer",
            NationalId = "1000000002"
        };

        var company1 = new Company()
        {
            UserId = co1.Id,
            Email = TestData.CompanyEmail,
            Name = "Test Logistics",
            TaxID = "300000000001"
        };
        var company2 = new Company()
        {
            UserId = co2.Id,
            Email = TestData.OtherCompanyEmail,
            Name = "Other Logistics",
            TaxID = "300000000002"
        };
        db.AddRange(customer1, customer2, company1, company2);
        await db.SaveChangesAsync();

        var kg = await db.WeightTypes.FirstAsync(wt => wt.Name == WeightTypeName.Kilogram);
        var receivedStatus = await db.ShipmentStatuses.FirstAsync(ss => ss.Name == ShipmentStatusName.OffersReceived);
        var submittedState = await db.OfferStates.FirstAsync(os => os.Name == OfferStateName.Submitted);
        var openStatus = await db.ShipmentStatuses.FirstAsync(ss => ss.Name == ShipmentStatusName.OpenForOffers);
        var awardedStatus = await db.ShipmentStatuses.FirstAsync(ss => ss.Name == ShipmentStatusName.Awarded);
        var inTransitStatus = await db.ShipmentStatuses.FirstAsync(ss => ss.Name == ShipmentStatusName.InTransit);
        var draftStatus = await db.ShipmentStatuses.FirstAsync(ss => ss.Name == ShipmentStatusName.Draft);
        var acceptedState = await db.OfferStates.FirstAsync(os => os.Name == OfferStateName.Accepted);
        var rejectedState = await db.OfferStates.FirstAsync(os => os.Name == OfferStateName.Rejected);

        // Drivers
        db.Drivers.AddRange(Fakers.Driver(company1.Id, 1).Generate(20));
        db.Drivers.AddRange(Fakers.Driver(company2.Id, 2).Generate(20));

        // Vehicles
        db.Vehicles.AddRange(Fakers.Vehicle(company1.Id, kg.Id, 1).Generate(30));
        db.Vehicles.AddRange(Fakers.Vehicle(company2.Id, kg.Id, 2).Generate(30)); // fixed: was company1

        // Received Shipments
        List<Shipment> customer1Shipments = [];
        List<Shipment> customer2Shipments = [];
        customer1Shipments.AddRange(Fakers.Shipment(customer1.Id, receivedStatus.Id, kg.Id, 1).Generate(50));
        customer2Shipments.AddRange(Fakers.Shipment(customer2.Id, receivedStatus.Id, kg.Id, 2).Generate(50));

        db.Shipments.AddRange(customer1Shipments);
        db.Shipments.AddRange(customer2Shipments);
        await db.SaveChangesAsync();

        // Received Shipments Offers
        List<Offer> offers = [];
        foreach (var shipment in customer1Shipments.Concat(customer2Shipments))
        {
            offers.AddRange(Fakers.Offer(company1.Id, shipment.Id, submittedState.Id, false, 1));
            offers.AddRange(Fakers.Offer(company2.Id, shipment.Id, submittedState.Id, false, 2));
        }

        db.Offers.AddRange(offers);
        await db.SaveChangesAsync();

        /////////////////////////////////////////////////

        // Draft Shipments
        List<Shipment> customer1DraftShipments = [];
        List<Shipment> customer2DraftShipments = [];
        customer1DraftShipments.AddRange(Fakers.Shipment(customer1.Id, draftStatus.Id, kg.Id, 1).Generate(50));
        customer2DraftShipments.AddRange(Fakers.Shipment(customer2.Id, draftStatus.Id, kg.Id, 2).Generate(50));

        db.Shipments.AddRange(customer1DraftShipments);
        db.Shipments.AddRange(customer2DraftShipments); // fixed: was customer1 twice
        await db.SaveChangesAsync();

        /////////////////////////////////////////////////

        // Open Shipments
        List<Shipment> customer1OpenShipments = [];
        List<Shipment> customer2OpenShipments = [];
        customer1OpenShipments.AddRange(Fakers.Shipment(customer1.Id, openStatus.Id, kg.Id, 1).Generate(50));
        customer2OpenShipments.AddRange(Fakers.Shipment(customer2.Id, openStatus.Id, kg.Id, 2).Generate(50));

        db.Shipments.AddRange(customer1OpenShipments);
        db.Shipments.AddRange(customer2OpenShipments); // fixed: was customer1 twice
        await db.SaveChangesAsync();

        ////////////////////////////////////////////////////

        // Awarded Shipments
        List<Shipment> customer1AwardedShipments = [];
        List<Shipment> customer2AwardedShipments = [];
        customer1AwardedShipments.AddRange(Fakers.Shipment(customer1.Id, awardedStatus.Id, kg.Id, 1).Generate(50));
        customer2AwardedShipments.AddRange(Fakers.Shipment(customer2.Id, awardedStatus.Id, kg.Id, 2).Generate(50));

        db.Shipments.AddRange(customer1AwardedShipments);
        db.Shipments.AddRange(customer2AwardedShipments);
        await db.SaveChangesAsync();

        // Awarded Shipments Offers
        List<Offer> awardedShipmentOffers = [];
        foreach (var shipment in customer1AwardedShipments.Concat(customer2AwardedShipments))
        {
            // fixed: was adding to `offers` (already saved), so these were never persisted
            awardedShipmentOffers.AddRange(Fakers.Offer(company1.Id, shipment.Id, rejectedState.Id, false, 1));
            awardedShipmentOffers.AddRange(Fakers.Offer(company2.Id, shipment.Id, acceptedState.Id, true, 2));
        }

        db.Offers.AddRange(awardedShipmentOffers);
        await db.SaveChangesAsync();

        ////////////////////////////////////////////////////

        // In-Transit Shipments
        List<Shipment> customer1InTransitShipments = [];
        List<Shipment> customer2InTransitShipments = [];
        customer1InTransitShipments.AddRange(Fakers.Shipment(customer1.Id, inTransitStatus.Id, kg.Id, 1).Generate(50));
        customer2InTransitShipments.AddRange(Fakers.Shipment(customer2.Id, inTransitStatus.Id, kg.Id, 2).Generate(50));

        db.Shipments.AddRange(customer1InTransitShipments);
        db.Shipments.AddRange(customer2InTransitShipments);
        await db.SaveChangesAsync();

        // In-Transit Shipments Offers
        List<Offer> inTransitShipmentOffers = [];
        foreach (var shipment in customer1InTransitShipments.Concat(customer2InTransitShipments))
        {
            // fixed: same bug as awarded offers
            inTransitShipmentOffers.AddRange(Fakers.Offer(company1.Id, shipment.Id, rejectedState.Id, false, 1));
            inTransitShipmentOffers.AddRange(Fakers.Offer(company2.Id, shipment.Id, acceptedState.Id, true, 2));
        }

        db.Offers.AddRange(inTransitShipmentOffers);
        await db.SaveChangesAsync();

        ////////////////////////////////////////////////////

        // Notifications
        await SeedNotificationsAsync(db, customer1.Id, customer1Shipments);
        await SeedNotificationsAsync(db, customer2.Id, customer2Shipments);
    }

    private static async Task SeedNotificationsAsync(
        CargoFlowDbContext db,
        int recipientId,                  // change to Guid if your profile ids are Guids
        List<Shipment> receivedShipments)
    {
        var now = DateTime.UtcNow;
        var notifications = new List<Notification>();

        for (var i = 0; i < 10; i++)
        {
            var shipment = receivedShipments[i];
            var createdAt = now.AddHours(-(i + 1)); // always older than anything a test creates

            notifications.Add(new Notification
            {
                RecipientId = recipientId,
                ShipmentId = shipment.Id,
                Type = NotificationType.OfferSubmitted,
                Title = "Offer Submitted",
                Message = $"New offer has been submitted successfully for shipment #{shipment.Id}",
                CreatedAt = createdAt,
                ReadAt = i < 8 ? null : createdAt.AddMinutes(5) // 8 unread, 2 read
            });
        }

        db.Notifications.AddRange(notifications);
        await db.SaveChangesAsync();
    }

    private static async Task<ApplicationUser> CreateUserAsync(UserManager<ApplicationUser> userManager, string email)
    {
        var user = new ApplicationUser()
        {
            Email = email,
            UserName = email
        };

        var result = await userManager.CreateAsync(user);
        if (!result.Succeeded)
        {
            throw new Exception(string.Join(",", result.Errors.Select(e => e.Description)));
        }

        return user;
    }
}
