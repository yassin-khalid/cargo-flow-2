using System.Net;
using System.Net.Http.Json;
using CargoFlow.Data;
using CargoFlow.Domain;
using CargoFlow.Dtos;
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

      var openStatus = await db.ShipmentStatuses.FirstAsync(ss => ss.Name == "Open for Offers");
      var receivedStatus = await db.ShipmentStatuses.FirstAsync(ss => ss.Name == "Offers Received");
      var weightType = await db.WeightTypes.FirstAsync(wt => wt.Name == "Ton (t)");
      
      var company = new Company()
         { Name = "Test Company", TaxID = "1234567890", Email = "test@company.com" };
      var company2 = new Company()
         { Name = "Test Company 2", TaxID = "3216549870", Email = "test@company2.com" };

      IEnumerable<Company> companies = [company, company2];
      
      db.Companies.AddRange(companies);
      
      var address1 =
         new Address() { Latitude = 21.4858M, Longitude = 39.1925M };
      var address2 =
         new Address() { Latitude = 24.0895M, Longitude = 38.0618M };
      
      IEnumerable<Address> addresses = [address1, address2];
      db.Addresses.AddRange(addresses);
      
      var customer = new Customer() { Name = "Customer 1", NationalId = "1234567890" };
      db.Customers.Add(customer);
      
      await db.SaveChangesAsync();

      var shipment = new Shipment()
      {
         ShipmentStatusId = openStatus.Id,
         PickupId = address1.Id,
         DestinationId = address2.Id,
         CustomerId = customer.Id,
         Weight = 200,
         WeightTypeId = weightType.Id
      };

      db.Shipments.Add(shipment);

      await db.SaveChangesAsync();

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
      var weightType = await db.WeightTypes.FirstAsync(wt => wt.Name == "Ton (t)");
      var submittedState = await db.OfferStates.FirstAsync(os => os.Name == "Submitted");
         
      var company = new Company()
         { Name = "Test Company 3", TaxID = "1234567891", Email = "test@company3.com" };
      var company2 = new Company()
         { Name = "Test Company 4", TaxID = "3216549871", Email = "test@company4.com" };

      IEnumerable<Company> companies = [company, company2];
         
      db.Companies.AddRange(companies);
         
      var address1 =
         new Address() { Latitude = 21.4858M, Longitude = 39.1925M };
      var address2 =
         new Address() { Latitude = 24.0895M, Longitude = 38.0618M };
         
      IEnumerable<Address> addresses = [address1, address2];
      db.Addresses.AddRange(addresses);
            
      var customer = new Customer() { Name = "Customer 2", NationalId = "1234567891" };
      db.Customers.Add(customer);

      await db.SaveChangesAsync();
         
      var shipment = new Shipment()
      {
         ShipmentStatusId = receivedStatus.Id,
         DestinationId = address1.Id,
         PickupId = address2.Id,
         Weight = 400,
         WeightTypeId = weightType.Id,
         CustomerId = customer.Id
      };

      db.Shipments.Add(shipment);
         
      await db.SaveChangesAsync();

      var submittedOffer = new Offer()
      {
         OfferStateId = submittedState.Id,
         CompanyId = company.Id,
         HasWon = false,
         Price = 400,
         ShipmentId = shipment.Id,
         SubmittedDate = DateTime.UtcNow,
      };

      db.Offers.Add(submittedOffer);
      await db.SaveChangesAsync();
      
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
      Assert.Equal(receivedStatus.Id, shipment.ShipmentStatusId);
   }

   [Fact]
   public async Task WithdrawOffer_AcceptedOrRejectedOffer_Returns409Conflict()
   {
      using var scope = factory.Services.CreateScope();
      var db = scope.ServiceProvider.GetRequiredService<CargoFlowDbContext>();
      var acceptedState = await db.OfferStates.FirstAsync(os => os.Name == "Accepted");
      var rejectedState = await db.OfferStates.FirstAsync(os => os.Name == "Rejected");
      var receivedStatus = await db.ShipmentStatuses.FirstAsync(ss => ss.Name == "Offers Received");
      var weightType = await db.WeightTypes.FirstAsync(wt => wt.Name == "Ton (t)");
         
      var company = new Company()
         { Name = "Test Company 5", TaxID = "1234567892", Email = "test@company5.com" };
      var company2 = new Company()
         { Name = "Test Company 6", TaxID = "3216549872", Email = "test@company6.com" };

      IEnumerable<Company> companies = [company, company2];
         
      db.Companies.AddRange(companies);
         
      var address1 =
         new Address() { Latitude = 21.4858M, Longitude = 39.1925M };
      var address2 =
         new Address() { Latitude = 24.0895M, Longitude = 38.0618M };
         
      IEnumerable<Address> addresses = [address1, address2];
      db.Addresses.AddRange(addresses);
            
      var customer = new Customer() { Name = "Customer 3", NationalId = "1234567892" };
      db.Customers.Add(customer);

      await db.SaveChangesAsync();
         
      var shipment = new Shipment()
      {
         ShipmentStatusId = receivedStatus.Id,
         DestinationId = address1.Id,
         PickupId = address2.Id,
         Weight = 400,
         WeightTypeId = weightType.Id,
         CustomerId = customer.Id
      };

      db.Shipments.Add(shipment);
         
      await db.SaveChangesAsync();

      var acceptedOffer = new Offer()
      {
         CompanyId = company.Id,
         ShipmentId = shipment.Id,
         Price = 400,
         OfferStateId = acceptedState.Id,
         HasWon = false,
         SubmittedDate = DateTime.UtcNow
      };

      var rejectedOffer = new Offer()
      {
         CompanyId = company2.Id,
         ShipmentId = shipment.Id,
         Price = 300,
         OfferStateId = rejectedState.Id,
         HasWon = false,
         SubmittedDate = DateTime.UtcNow
      };
         
      db.Offers.AddRange(acceptedOffer, rejectedOffer);
      await db.SaveChangesAsync();

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
      var submittedState = await db.OfferStates.FirstAsync(os => os.Name == "Submitted");
      var receivedState = await db.ShipmentStatuses.FirstAsync(ss => ss.Name == "Offers Received");
      var weightType = await db.WeightTypes.FirstAsync(wt => wt.Name == "Ton (t)");
         
      var company = new Company()
         { Name = "Test Company 7", TaxID = "1234567893", Email = "test@company7.com" };
      var company2 = new Company()
         { Name = "Test Company 8", TaxID = "3216549873", Email = "test@company8.com" };

      IEnumerable<Company> companies = [company, company2];
         
      db.Companies.AddRange(companies);
         
      var address1 =
         new Address() { Latitude = 21.4858M, Longitude = 39.1925M };
      var address2 =
         new Address() { Latitude = 24.0895M, Longitude = 38.0618M };
         
      IEnumerable<Address> addresses = [address1, address2];
      db.Addresses.AddRange(addresses);
            
      var customer = new Customer() { Name = "Customer 4", NationalId = "1234567893" };
      db.Customers.Add(customer);

      await db.SaveChangesAsync();

      var shipment = new Shipment()
      {
         CustomerId = customer.Id,
         Weight = 550,
         WeightTypeId = weightType.Id,
         DestinationId = address1.Id,
         PickupId = address2.Id,
         ShipmentStatusId = receivedState.Id,
      };

      db.Shipments.Add(shipment);
      await db.SaveChangesAsync();

      var offer = new Offer()
      {
         CompanyId = company.Id,
         HasWon = false,
         OfferStateId = submittedState.Id,
         Price = 700,
         ShipmentId = shipment.Id,
         SubmittedDate = DateTime.UtcNow
      };

      db.Offers.Add(offer);
      await db.SaveChangesAsync();

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
      
      var submittedState = await db.OfferStates.FirstAsync(os => os.Name == "Submitted");
      var receivedStatus = await db.ShipmentStatuses.FirstAsync(ss => ss.Name == "Offers Received");
      var weightType = await db.WeightTypes.FirstAsync(wt => wt.Name == "Ton (t)");
         
      var company = new Company()
         { Name = "Test Company 9", TaxID = "1234567894", Email = "test@company9.com" };

      db.Companies.Add(company);
         
      var address1 =
         new Address() { Latitude = 21.4858M, Longitude = 39.1925M };
      var address2 =
         new Address() { Latitude = 24.0895M, Longitude = 38.0618M };
         
      IEnumerable<Address> addresses = [address1, address2];
      db.Addresses.AddRange(addresses);
            
      var customer = new Customer() { Name = "Customer 5", NationalId = "1234567894" };
      db.Customers.Add(customer);

      await db.SaveChangesAsync();

      var shipment = new Shipment()
      {
         CustomerId = customer.Id,
         Weight = 666,
         WeightTypeId = weightType.Id,
         DestinationId = address1.Id,
         PickupId = address2.Id,
         ShipmentStatusId = receivedStatus.Id,
      };

      db.Shipments.Add(shipment);
      await db.SaveChangesAsync();

      var offer = new Offer()
      {
         SubmittedDate = DateTime.UtcNow,
         Price = 780,
         HasWon = false,
         OfferStateId = submittedState.Id,
         CompanyId = company.Id,
         ShipmentId = shipment.Id
      };
      
      db.Offers.Add(offer);
      await db.SaveChangesAsync();

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
   
}