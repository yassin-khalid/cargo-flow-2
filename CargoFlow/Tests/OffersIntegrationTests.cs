using System.Net;
using CargoFlow.Data;
using CargoFlow.Domain;
using Xunit;

namespace CargoFlow.Tests;

public class OffersIntegrationTests(LocalSqlWebApplicationFactory<Program> factory)
   : IClassFixture<LocalSqlWebApplicationFactory<Program>>
{
   private readonly HttpClient _client = factory.CreateClient();


   [Fact]
   public async Task SubmitOffer_FirstOffer_Returns201CreatedAndTransitionsShipmentStatus()
   {
      using var scope = factory.Services.CreateScope();
      var db = scope.ServiceProvider.GetRequiredService<CargoFlowDbContext>();

      var openStatus = new ShipmentStatus() { Name = "Open For Offers" };
      var receivedStatus = new ShipmentStatus() { Name = "Offers Received" };
      var submittedState = new OfferState() { Name = "Submitted" };
      var weightType = new WeightType() { Name = "Ton" };
      
      db.ShipmentStatuses.AddRange(openStatus, receivedStatus);
      db.OfferStates.Add(submittedState);
      db.WeightTypes.Add(weightType);

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