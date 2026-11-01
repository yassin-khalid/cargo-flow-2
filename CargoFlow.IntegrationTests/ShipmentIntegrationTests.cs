using System.Net;
using System.Net.Http.Json;
using CargoFlow.Data;
using CargoFlow.IntegrationTests.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CargoFlow.IntegrationTests;

[Collection("Database collection")]
public class ShipmentIntegrationTests(LocalSqlWebApplicationFactory<Program> factory) 
{
   private readonly HttpClient _client = factory.CreateClient();
    
    
   [Fact]
   public async Task CancelShipment_ValidShipments_Returns204AndTransitionsShipmentStatus()
   {
      using var scope = factory.Services.CreateScope();
      var db = scope.ServiceProvider.GetRequiredService<CargoFlowDbContext>();

      var shipments = await ShipmentSeeder.SeedShipmentListWithUniqueStatusAsync(db, [
         new ShipmentSpec("Draft", "Ton (t)", 30),
         new ShipmentSpec("Open for Offers", "Ton (t)", 30),
         new ShipmentSpec("Offers Received", "Ton (t)", 30),
      ]);

      var draftResponse = await _client.PostAsJsonAsync($"/api/shipments/{shipments["Draft"].Id}/cancel", new { });
      Assert.Equal(HttpStatusCode.NoContent, draftResponse.StatusCode);
      
      var openResponse = await _client.PostAsJsonAsync($"/api/shipments/{shipments["Open for Offers"].Id}/cancel", new { });
      Assert.Equal(HttpStatusCode.NoContent, openResponse.StatusCode);
      
      var receivedResponse = await _client.PostAsJsonAsync($"/api/shipments/{shipments["Offers Received"].Id}/cancel", new { });
      Assert.Equal(HttpStatusCode.NoContent, receivedResponse.StatusCode);
   
      using var verifyScope = factory.Services.CreateScope();
      var verifyDb = verifyScope.ServiceProvider.GetRequiredService<CargoFlowDbContext>();

      var shipmentIds = shipments.Values.Select(v => v.Id).ToList();

      var shipmentsStatuses = await verifyDb.Shipments
         .Include(s => s.ShipmentStatus)
         .Where(s => shipmentIds.Contains(s.Id))
         .Select(s => s.ShipmentStatus.Name)
         .ToListAsync();
      
      Assert.All(shipmentsStatuses, s => Assert.Equal("Cancelled", s));
   }

   [Fact]
   public async Task CancelShipment_InvalidShipments_Returns409Conflict()
   {
      using var scope = factory.Services.CreateScope();
      var db = scope.ServiceProvider.GetRequiredService<CargoFlowDbContext>();

      var shipments = await ShipmentSeeder.SeedShipmentListWithUniqueStatusAsync(db, [
         new ShipmentSpec("Awarded", "Ton (t)", 20),
         new ShipmentSpec("In Transit", "Ton (t)", 20),
         new ShipmentSpec("Delivered", "Ton (t)", 20),
         new ShipmentSpec("Completed", "Ton (t)", 20),
         new ShipmentSpec("Cancelled", "Ton (t)", 20),
      ]);

      var shipmentIds = shipments.Values.Select(s => s.Id).ToList();
      foreach (var shipmentId in shipmentIds)
      {
         var response = await _client.PostAsJsonAsync($"/api/shipments/{shipmentId}/cancel", new { });
         Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
      }

      using var verifyScope = factory.Services.CreateScope();
      var verifyDb = verifyScope.ServiceProvider.GetRequiredService<CargoFlowDbContext>();

      var result = await verifyDb.Shipments
         .Include(s => s.ShipmentStatus)
         .Where(s => shipmentIds.Contains(s.Id))
         .ToListAsync();
      
      Assert.Equal(shipmentIds.Count, result.Count);

      foreach (var s in result)
      {
         Assert.True(shipments.TryGetValue(s.ShipmentStatus.Name, out var original));
         Assert.Equal(original.Id, s.Id);
      }
   }

   [Fact]
   public async Task CancelShipment_MissingShipments_Returns404NotFound()
   {
      using var scope = factory.Services.CreateScope();
      var db = scope.ServiceProvider.GetRequiredService<CargoFlowDbContext>();

      var maxShipmentId = await db.Shipments.MaxAsync(s => s.Id);
      var nonExistenceShipmentId = maxShipmentId + 100;

      var response = await _client.PostAsJsonAsync($"/api/shipments/{nonExistenceShipmentId}/cancel", new { });
      
      Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
   }

   [Fact]
   public async Task CancelShipment_ShipmentOffersRemainsUnchanges()
   {
      using var scope = factory.Services.CreateScope();
      var db = scope.ServiceProvider.GetRequiredService<CargoFlowDbContext>();

      var (shipment, offers) = await ShipmentSeeder.SeedShipmentWithOffersAsync(db, "Offers Received", "Ton (t)", 20, [
         new OfferSpec("Test Company", 1200, "Accepted", true),
         new OfferSpec("Test Company", 1300, "Submitted"),
         new OfferSpec("Test Company", 1100, "Rejected"),
         new OfferSpec("Test Company", 1000, "Expired"),
         new OfferSpec("Test Company", 1400, "Withdrawn")
      ]);

      var response = await _client.PostAsJsonAsync($"/api/shipments/{shipment.Id}/cancel", new { });
      
      Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

      var offerIds = offers.Select(o => o.Id).ToList();
      using var verifyScope = factory.Services.CreateScope();
      var verifyDb = verifyScope.ServiceProvider.GetRequiredService<CargoFlowDbContext>();

      var dbOffers = await verifyDb.Offers
         .Where(o => offerIds.Contains(o.Id))
         .ToListAsync();
      
      Assert.Equal(dbOffers.Count, offers.Count);
      Assert.All(dbOffers, dbo => Assert.Equal(offers
         .First(o => o.Id == dbo.Id).OfferStateId, dbo.OfferStateId));
   }

   [Fact]
   public async Task CancelShipment_ConcurrentRequests_ShouldResultInConsistentState()
   {
      using var scope = factory.Services.CreateScope();
      var db = scope.ServiceProvider.GetRequiredService<CargoFlowDbContext>();

      var shipment = await ShipmentSeeder.SeedShipmentListWithUniqueStatusAsync(db, [
         new ShipmentSpec("Draft", "Ton (t)", 200)
      ]);

      var firstCancelRequest = _client.PostAsJsonAsync($"/api/shipments/{shipment["Draft"].Id}/cancel", new { });
      var secondCancelRequest = _client.PostAsJsonAsync($"/api/shipments/{shipment["Draft"].Id}/cancel", new { });

      var responses = await Task.WhenAll(firstCancelRequest, secondCancelRequest);

      var successCount = responses.Count(r => r.StatusCode == HttpStatusCode.NoContent);
      var conflictCount = responses.Count(r => r.StatusCode == HttpStatusCode.Conflict);
      
      Assert.Equal(1, successCount);
      Assert.True(conflictCount >= 1);
   }
}