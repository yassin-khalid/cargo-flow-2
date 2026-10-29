using System.Net;
using System.Net.Http.Json;
using CargoFlow.Data;
using CargoFlow.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

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
      var submittedState = await db.OfferStates.FirstAsync(os => os.Name == "Submitted");
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
      {
         CompanyId = company.Id,
         Price = 500
      });
      
      Assert.Equal(HttpStatusCode.Created,firstOfferResponse.StatusCode);

      var duplicateOfferResponse = await _client.PostAsJsonAsync($"/api/shipments/{shipment.Id}/offers", new
      {
         CompanyId = company.Id,
         Price = 500
      });
      
      Assert.Equal(HttpStatusCode.Conflict, duplicateOfferResponse.StatusCode);

      var secondCompanyOfferResponse = await _client.PostAsJsonAsync($"/api/shipments/{shipment.Id}/offers", new
      {
         CompanyId = company2.Id,
         Price = 400
      });
      
      Assert.Equal(HttpStatusCode.Created, secondCompanyOfferResponse.StatusCode);

   }
}