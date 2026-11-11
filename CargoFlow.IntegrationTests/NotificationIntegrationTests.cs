using System.Net;
using System.Net.Http.Json;
using CargoFlow.Data;
using CargoFlow.Domain.Enums;
using CargoFlow.Dtos;
using CargoFlow.IntegrationTests.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CargoFlow.IntegrationTests;

[Collection("Database collection")]
public class NotificationIntegrationTests(LocalSqlWebApplicationFactory<Program> factory)
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task AwardOffer_WhenSuccess_ShouldCreateCompanyNotification()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CargoFlowDbContext>();

        var (shipment, offers) = await ShipmentSeeder.SeedShipmentWithOffersAsync(db, ShipmentStatusName.OffersReceived, "Ton (t)", 200, [
            new("Test Company", 100, OfferStateName.Submitted)
        ]);

        var response = await _client.PostAsJsonAsync($"/api/shipments/{shipment.Id}/offers/{offers[0].Id}/award", new { });
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        using var verifyScope = factory.Services.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<CargoFlowDbContext>();

        var notification = await verifyDb.Notifications
            .Where(n => n.RecipientId == offers[0].CompanyId
                && n.Type == NotificationType.OfferAwarded
                && n.RecipientType == RecipientType.Company
                && n.ShipmentId == shipment.Id
            )
            .ToListAsync();

        Assert.NotEmpty(notification);
    }

    [Fact]
    public async Task SubmitOffer_WhenSuccess_ShouldCreateCustomerNotification()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CargoFlowDbContext>();

        var shipmentDic = await ShipmentSeeder.SeedShipmentListWithUniqueStatusAsync(db, [
            new(ShipmentStatusName.OpenForOffers, "Ton (t)", 200)
        ]);

        var companies = await CompanySeeder.GenerateCompaniesAsync(db, 1);
        var companyId = companies[0].Id;

        var shipment = shipmentDic.Values.ElementAt(0);

        var response = await _client.PostAsJsonAsync(
            $"/api/shipments/{shipment.Id}/offers",
            new SubmitOfferRequestBody(companyId, 100));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        using var verifyScope = factory.Services.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<CargoFlowDbContext>();

        var notification = await verifyDb.Notifications
            .Where(n => n.ShipmentId == shipment.Id
                && n.RecipientId == shipment.CustomerId
                && n.RecipientType == RecipientType.Customer
                && n.Type == NotificationType.OfferSubmitted
            )
            .ToListAsync();

        Assert.NotEmpty(notification);

    }
}
