using System.Net; 
using System.Net.Http.Json;
using System.Text.Json;
using CargoFlow.Data;
using CargoFlow.Domain;
using CargoFlow.Domain.Enums;
using CargoFlow.Dtos;
using CargoFlow.Endpoints;
using CargoFlow.Helpers;
using CargoFlow.IntegrationTests.Helpers;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;

namespace CargoFlow.IntegrationTests;

[Collection("Database collection")]
public class ShipmentStatusHistoryIntegrationTest(LocalSqlWebApplicationFactory<Program> factory, ITestOutputHelper testOutputHelper)
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task CreateShipment_AddedDraftStatusHistory()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CargoFlowDbContext>();

        var dto = await ShipmentSeeder.GetValidCreateShipmentDtoAsync(db);
        var result = await _client.PostAsJsonAsync($"/api/shipments", dto, new JsonSerializerOptions()
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
        });

        Assert.Equal(HttpStatusCode.Created, result.StatusCode);
        
        var responseDto = await result.Content.ReadFromJsonAsync<ShipmentDto>();
        Assert.NotNull(responseDto);

        using var verifyScope = factory.Services.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<CargoFlowDbContext>();
        var verifyData = verifyScope.ServiceProvider.GetRequiredService<IReferenceData>();
        
        var draftStatusId = verifyData.GetShipmentStatusId(ShipmentStatusName.Draft);
        var ssh = await verifyDb.ShipmentStatusHistories.FirstOrDefaultAsync(ssh => ssh.ShipmentId == responseDto.Id);
        Assert.NotNull(ssh);
        Assert.Equal(draftStatusId, ssh.ShipmentStatusId);
    }

    [Fact]
    public async Task OpenShipment_ShouldAddOpenedStatusHistory()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CargoFlowDbContext>();

        var shipmentDict = await ShipmentSeeder.SeedShipmentListWithUniqueStatusAsync(db, [
            new ShipmentSpec(ShipmentStatusName.Draft, "Ton (t)", 20),
        ]);
        var shipment = shipmentDict.Values.Single();
        
        var response = await _client.PostAsJsonAsync($"/api/shipments/{shipment.Id}/open-for-offers", new {});

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        using var verifyScope = factory.Services.CreateScope();
        var verifyDb = factory.Services.GetRequiredService<CargoFlowDbContext>();
        var referenceData = verifyScope.ServiceProvider.GetRequiredService<IReferenceData>();
        var openStatus = referenceData.GetShipmentStatusId(ShipmentStatusName.OpenForOffers);

        var ssh = await verifyDb.ShipmentStatusHistories.FirstOrDefaultAsync(ssHistory =>
            ssHistory.ShipmentId == shipment.Id && ssHistory.ShipmentStatusId == openStatus);

        Assert.NotNull(ssh);
    }
    
    [Fact]
    public async Task CancelShipment_ShouldAddCancelledStatusHistory()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CargoFlowDbContext>();

        var shipmentDict = await ShipmentSeeder.SeedShipmentListWithUniqueStatusAsync(db, [
            new ShipmentSpec(ShipmentStatusName.Draft, "Ton (t)", 20),
            new ShipmentSpec(ShipmentStatusName.OpenForOffers, "Ton (t)", 20),
            new ShipmentSpec(ShipmentStatusName.OffersReceived, "Ton (t)", 20),
        ]);

        var shipments = shipmentDict.Values.ToList();
        var ids = shipments.Select(s => s.Id).ToList();

        foreach (var id in ids)
        {
            var response = await _client.PostAsJsonAsync($"/api/shipments/{id}/cancel", new {});
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }

        using var verifyScope = factory.Services.CreateScope();
        var verifyDb = factory.Services.GetRequiredService<CargoFlowDbContext>();
        var referenceData = verifyScope.ServiceProvider.GetRequiredService<IReferenceData>();
        var cancelledStatusId = referenceData.GetShipmentStatusId(ShipmentStatusName.Cancelled);

        foreach (var id in ids)
        {
            
            var ssh = await verifyDb.ShipmentStatusHistories.FirstOrDefaultAsync(ssHistory =>
                ssHistory.ShipmentId == id && ssHistory.ShipmentStatusId == cancelledStatusId);

            Assert.NotNull(ssh);
        }
    }
    
    [Fact]
    public async Task StartShipment_ShouldAddStartedStatusHistory()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CargoFlowDbContext>();

        var shipmentDict = await ShipmentSeeder.SeedShipmentListWithUniqueStatusAsync(db, [
            new ShipmentSpec(ShipmentStatusName.Awarded, "Ton (t)", 20),
        ]);

        var shipment = shipmentDict.Values.Single();

        var response = await _client.PostAsJsonAsync($"/api/shipments/{shipment.Id}/start", new { });
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        using var verifyScope = factory.Services.CreateScope();
        var verifyDb = factory.Services.GetRequiredService<CargoFlowDbContext>();
        var referenceData = verifyScope.ServiceProvider.GetRequiredService<IReferenceData>();
        var inTransitStatusId = referenceData.GetShipmentStatusId(ShipmentStatusName.InTransit);


        var ssh = await verifyDb.ShipmentStatusHistories.FirstOrDefaultAsync(ssHistory =>
            ssHistory.ShipmentId == shipment.Id && ssHistory.ShipmentStatusId == inTransitStatusId);

        Assert.NotNull(ssh);
    }

    [Fact]
    public async Task DeliverShipment_ShouldAddDeliveredStatusHistory()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CargoFlowDbContext>();

        var shipmentDict = await ShipmentSeeder.SeedShipmentListWithUniqueStatusAsync(db, [
            new ShipmentSpec(ShipmentStatusName.InTransit, "Ton (t)", 20),
        ]);

        var shipment = shipmentDict.Values.Single();

        var response = await _client.PostAsJsonAsync($"/api/shipments/{shipment.Id}/deliver", new { });
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        using var verifyScope = factory.Services.CreateScope();
        var verifyDb = factory.Services.GetRequiredService<CargoFlowDbContext>();
        var referenceData = verifyScope.ServiceProvider.GetRequiredService<IReferenceData>();
        var deliveredStatusId = referenceData.GetShipmentStatusId(ShipmentStatusName.Delivered);


        var ssh = await verifyDb.ShipmentStatusHistories.FirstOrDefaultAsync(ssHistory =>
            ssHistory.ShipmentId == shipment.Id && ssHistory.ShipmentStatusId == deliveredStatusId);

        Assert.NotNull(ssh);       
    }

    [Fact]
    public async Task SubmitOffer_ShouldAddOpenedStatus()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CargoFlowDbContext>();

        var shipmentsDict = await ShipmentSeeder.SeedShipmentListWithUniqueStatusAsync(db, [
                new ShipmentSpec(ShipmentStatusName.OpenForOffers, "Ton (t)", 100),
            ]
        );

        var company = new Company()
        {
            Email = "test@company.com",
            TaxID = "TAX-123454",
            Name = "Test Company 2"
        };

        db.Companies.Add(company);
        await db.SaveChangesAsync();

        var shipment = shipmentsDict.Values.First();

        var result = await _client.PostAsJsonAsync($"/api/shipments/{shipment.Id}/offers",
            new SubmitOfferRequestBody(company.Id, 100));
        Assert.Equal(HttpStatusCode.Created, result.StatusCode);

        using var verifyScope= factory.Services.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<CargoFlowDbContext>();
        var referenceData = verifyScope.ServiceProvider.GetRequiredService<IReferenceData>();
        var receivedStatus = referenceData.GetShipmentStatusId(ShipmentStatusName.OffersReceived);

        var ssHistory = await verifyDb.ShipmentStatusHistories
            .FirstOrDefaultAsync(ssh => ssh.ShipmentId == shipment.Id && ssh.ShipmentStatusId == receivedStatus);
        
        Assert.NotNull(ssHistory);
    }
    
    [Fact]
    public async Task AwardOffer_ShouldAddAwardedStatus()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CargoFlowDbContext>();

        var (shipment, offers) = await ShipmentSeeder.SeedShipmentWithOffersAsync(
            db, ShipmentStatusName.OffersReceived, "Ton (t)", 100, [
                new OfferSpec("test company", 1000, OfferStateName.Submitted)
            ]);

        var result = await _client.PostAsJsonAsync($"/api/shipments/{shipment.Id}/offers/{offers[0].Id}/award", new { });
        Assert.Equal(HttpStatusCode.NoContent, result.StatusCode);

        using var verifyScope = factory.Services.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<CargoFlowDbContext>();
        var referenceData = verifyScope.ServiceProvider.GetRequiredService<IReferenceData>();
        var awardedStatus = referenceData.GetShipmentStatusId(ShipmentStatusName.Awarded);

        var ssHistory = await verifyDb.ShipmentStatusHistories
            .FirstOrDefaultAsync(ssh => shipment.Id == ssh.ShipmentId && ssh.ShipmentStatusId == awardedStatus);

        Assert.NotNull(ssHistory);
    }
}