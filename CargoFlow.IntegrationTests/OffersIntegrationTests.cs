using System.Net;
using System.Net.Http.Json;
using CargoFlow.Data;
using CargoFlow.Dtos;
using CargoFlow.IntegrationTests.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CargoFlow.IntegrationTests;

public class OffersIntegrationTests(LocalSqlWebApplicationFactory<Program> factory)
   : IClassFixture<LocalSqlWebApplicationFactory<Program>>
{
   private readonly HttpClient _client = factory.CreateClient();


   [Fact]
   public async Task SubmitOffer_FirstOffer_Returns201CreatedAndTransitionsShipmentStatus()
   {
      using var scope = factory.Services.CreateScope();
      var db = scope.ServiceProvider.GetRequiredService<CargoFlowDbContext>();

      var receivedStatus = await db.ShipmentStatuses.FirstAsync(ss => ss.Name == "Offers Received");

      var companies = await CompanySeeder.GenerateCompaniesAsync(db, 2);

      var company = companies[0];
      var company2 = companies[1];
      
      var (shipment, _) = await ShipmentSeeder.SeedShipmentsWithOffersAsync(
         db, 
         "Open for Offers", 
         "Ton (t)", 
         3, 
         []);

      var firstOfferResponse = await _client.PostAsJsonAsync($"/api/shipments/{shipment.Id}/offers", new
         SubmitOfferRequestBody(company.Id, 500));

      Assert.Equal(HttpStatusCode.Created,firstOfferResponse.StatusCode);

      using (var verifyScope = factory.Services.CreateScope())
      {
         var verifyDb = verifyScope.ServiceProvider.GetRequiredService<CargoFlowDbContext>();
         var updatedShipment = await verifyDb.Shipments.FirstOrDefaultAsync(s => s.Id == shipment.Id);
         Assert.NotNull(updatedShipment);
         Assert.Equal(receivedStatus.Id, updatedShipment.ShipmentStatusId);
      }
      

      var duplicateOfferResponse = await _client.PostAsJsonAsync($"/api/shipments/{shipment.Id}/offers",
         new SubmitOfferRequestBody(company.Id, 500));
      
      Assert.Equal(HttpStatusCode.Conflict, duplicateOfferResponse.StatusCode);

      var secondCompanyOfferResponse = await _client.PostAsJsonAsync($"/api/shipments/{shipment.Id}/offers",
         new SubmitOfferRequestBody(company2.Id, 400));
      
      Assert.Equal(HttpStatusCode.Created, secondCompanyOfferResponse.StatusCode);

   }

   [Fact]
   public async Task WithdrawOffer_SubmittedOffer_Returns204AndTransitionsOfferState()
   {
      using var scope = factory.Services.CreateScope();
      var db = scope.ServiceProvider.GetRequiredService<CargoFlowDbContext>();
      var withdrawnState = await db.OfferStates.FirstAsync(os => os.Name == "Withdrawn");
      var receivedStatus = await db.ShipmentStatuses.FirstAsync(ss => ss.Name == "Offers Received");
      
      var (shipment, offers) = await ShipmentSeeder.SeedShipmentsWithOffersAsync(db, "Offers Received", "Ton (t)", 40, [
         new OfferSpec("Test Company", 400, "Submitted")
      ]);

      var submittedOffer = offers[0];
      
      var withdrawOfferResponse = await _client
         .PostAsJsonAsync($"/api/shipments/{shipment.Id}/offers/{submittedOffer.Id}/withdraw", new {});

      Assert.Equal(HttpStatusCode.NoContent, withdrawOfferResponse.StatusCode);

      using var verifyScope = factory.Services.CreateScope();
      var verifyDb = verifyScope.ServiceProvider.GetRequiredService<CargoFlowDbContext>();
      var updatedOffer = await verifyDb.Offers.FindAsync(submittedOffer.Id);
      Assert.NotNull(updatedOffer);
      Assert.Equal(withdrawnState.Id, updatedOffer.OfferStateId);

      var offerShipment = await verifyDb.Shipments.FindAsync(shipment.Id);
      Assert.NotNull(offerShipment);
      Assert.Equal(receivedStatus.Id, offerShipment.ShipmentStatusId);
   }

   [Fact]
   public async Task WithdrawOffer_AcceptedOrRejectedOffer_Returns409Conflict()
   {
      using var scope = factory.Services.CreateScope();
      var db = scope.ServiceProvider.GetRequiredService<CargoFlowDbContext>();
      var acceptedState = await db.OfferStates.FirstAsync(os => os.Name == "Accepted");
      var rejectedState = await db.OfferStates.FirstAsync(os => os.Name == "Rejected");

      var (shipment, offers) = await ShipmentSeeder.SeedShipmentsWithOffersAsync(db, "Offers Received", "Ton (t)", 300,
      [
         new OfferSpec("Test Company 1", 500, "Accepted", true),
         new OfferSpec("Test Company 2", 400, "Rejected"),
      ]);

      var acceptedOffer = offers.First(offer => offer.OfferStateId == acceptedState.Id);
      var rejectedOffer = offers.First(offer => offer.OfferStateId == rejectedState.Id);
      
      var acceptedOfferResponse =
         await _client.PostAsJsonAsync($"/api/shipments/{shipment.Id}/offers/{acceptedOffer.Id}/withdraw", new { });
         
      Assert.Equal(HttpStatusCode.Conflict, acceptedOfferResponse.StatusCode);
         
         
      var rejectedOfferResponse =
         await _client.PostAsJsonAsync($"/api/shipments/{shipment.Id}/offers/{rejectedOffer.Id}/withdraw", new { });
         
      Assert.Equal(HttpStatusCode.Conflict, rejectedOfferResponse.StatusCode);
   }

   [Fact]
   public async Task AwardOffer_ValidOffer_Returns204AndOfferWins()
   {
      using var scope = factory.Services.CreateScope();
      var db = scope.ServiceProvider.GetRequiredService<CargoFlowDbContext>();
      var (shipment, offers) = await ShipmentSeeder.SeedShipmentsWithOffersAsync(db, "Offers Received", "Ton (t)", 200,
      [
         new OfferSpec("Test Company", 1400, "Submitted")
      ]);

      var offer = offers[0];
      
      var awardValidOfferResponse =
         await _client.PostAsJsonAsync($"/api/shipments/{shipment.Id}/offers/{offer.Id}/award", new { });
         
      Assert.Equal(HttpStatusCode.NoContent, awardValidOfferResponse.StatusCode);

      using var verifyScope = factory.Services.CreateScope();
      var verifyDb = verifyScope.ServiceProvider.GetRequiredService<CargoFlowDbContext>();
      var updatedOffer = await verifyDb.Offers.FirstOrDefaultAsync(o => o.Id == offer.Id);
      Assert.NotNull(updatedOffer);
      Assert.True(updatedOffer.HasWon);
   }

   [Fact]
   public async Task Concurrent_AwardAndWithdraw_ShouldResultInConsistentState()
   {
      using var scope = factory.Services.CreateScope();
      var db = scope.ServiceProvider.GetRequiredService<CargoFlowDbContext>();

      var (shipment, offers) = await ShipmentSeeder.SeedShipmentsWithOffersAsync(db, "Offers Received", "Ton (t)", 300,
      [
         new OfferSpec("Test Company", 3000, "Submitted")
      ]);

      var offer = offers[0];
      
      var awardOffer = _client.PostAsJsonAsync($"/api/shipments/{shipment.Id}/offers/{offer.Id}/award", new { });
      var withdrawOffer = _client.PostAsJsonAsync($"api/shipments/{shipment.Id}/offers/{offer.Id}/withdraw", new {});

      var responses = await Task.WhenAll(awardOffer, withdrawOffer);

      var awardResponse = responses[0];
      var withdrawResponse = responses[1];

      var successCount = responses.Count(r => r.IsSuccessStatusCode);
      var conflictCount = responses.Count(r => r.StatusCode == HttpStatusCode.Conflict);
      
      Assert.Equal(1, successCount);
      Assert.True(conflictCount >= 1);

      using var verifyScope = factory.Services.CreateScope();
      var verifyDb = verifyScope.ServiceProvider.GetRequiredService<CargoFlowDbContext>();
      var finalOffer = await verifyDb.Offers
         .Include(o => o.OfferState)
         .FirstAsync(o => o.Id == offer.Id);

      if (awardResponse.IsSuccessStatusCode)
      {
         Assert.True(finalOffer.HasWon);
         Assert.Equal("Accepted", finalOffer.OfferState.Name);
      }

      if (withdrawResponse.IsSuccessStatusCode)
      {
         Assert.False(finalOffer.HasWon);
         Assert.Equal("Withdrawn", finalOffer.OfferState.Name);
      }

   }

   [Fact]
   public async Task AwardOffer_AlreadyWithdrawnOffer_Returns409Conflict()
   {
      using var scope = factory.Services.CreateScope();
      var db = scope.ServiceProvider.GetRequiredService<CargoFlowDbContext>();
      var (shipment, offers) = await ShipmentSeeder.SeedShipmentsWithOffersAsync(
         db, 
         "Offers Received", 
         "Ton (t)", 
         500, [
         new OfferSpec("Test Company 1", 500, "Withdrawn")
      ]);

      var response =
         await _client.PostAsJsonAsync($"/api/shipments/{shipment.Id}/offers/{offers[0].Id}/award", new { });
      
      Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

   }
}