using System.Net;
using System.Net.Http.Json;
using CargoFlow.Data;
using CargoFlow.Domain.Enums;
using CargoFlow.Dtos;
using CargoFlow.Endpoints;
using CargoFlow.Helpers;
using CargoFlow.IntegrationTests.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;

namespace CargoFlow.IntegrationTests;

[Collection("Database collection")]
public class ShipmentIntegrationTests(LocalSqlWebApplicationFactory<Program> factory, ITestOutputHelper testOutputHelper) 
{
   private readonly HttpClient _client = factory.CreateClient();
    
    
   [Fact]
   public async Task CancelShipment_ValidShipments_Returns204AndTransitionsShipmentStatus()
   {
      using var scope = factory.Services.CreateScope();
      var db = scope.ServiceProvider.GetRequiredService<CargoFlowDbContext>();

      var shipments = await ShipmentSeeder.SeedShipmentListWithUniqueStatusAsync(db, [
         new ShipmentSpec(ShipmentStatusName.Draft, "Ton (t)", 30),
         new ShipmentSpec(ShipmentStatusName.OpenForOffers, "Ton (t)", 30),
         new ShipmentSpec(ShipmentStatusName.OffersReceived, "Ton (t)", 30),
      ]);

      var draftResponse = await _client.PostAsJsonAsync($"/api/shipments/{shipments[ShipmentStatusName.Draft].Id}/cancel", new { });
      Assert.Equal(HttpStatusCode.NoContent, draftResponse.StatusCode);
      
      var openResponse = await _client.PostAsJsonAsync($"/api/shipments/{shipments[ShipmentStatusName.OpenForOffers].Id}/cancel", new { });
      Assert.Equal(HttpStatusCode.NoContent, openResponse.StatusCode);
      
      var receivedResponse = await _client.PostAsJsonAsync($"/api/shipments/{shipments[ShipmentStatusName.OffersReceived].Id}/cancel", new { });
      Assert.Equal(HttpStatusCode.NoContent, receivedResponse.StatusCode);
   
      using var verifyScope = factory.Services.CreateScope();
      var verifyDb = verifyScope.ServiceProvider.GetRequiredService<CargoFlowDbContext>();

      var shipmentIds = shipments.Values.Select(v => v.Id).ToList();

      var shipmentsStatuses = await verifyDb.Shipments
         .Include(s => s.ShipmentStatus)
         .Where(s => shipmentIds.Contains(s.Id))
         .Select(s => s.ShipmentStatus.Name)
         .ToListAsync();
      
      Assert.All(shipmentsStatuses, s => Assert.Equal(ShipmentStatusName.Cancelled, s));
   }

   [Fact]
   public async Task CancelShipment_InvalidShipments_Returns409Conflict()
   {
      using var scope = factory.Services.CreateScope();
      var db = scope.ServiceProvider.GetRequiredService<CargoFlowDbContext>();

      var shipments = await ShipmentSeeder.SeedShipmentListWithUniqueStatusAsync(db, [
         new ShipmentSpec(ShipmentStatusName.Awarded, "Ton (t)", 20),
         new ShipmentSpec(ShipmentStatusName.InTransit, "Ton (t)", 20),
         new ShipmentSpec(ShipmentStatusName.Delivered, "Ton (t)", 20),
         new ShipmentSpec(ShipmentStatusName.Completed, "Ton (t)", 20),
         new ShipmentSpec(ShipmentStatusName.Cancelled, "Ton (t)", 20),
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

      var (shipment, offers) = await ShipmentSeeder.SeedShipmentWithOffersAsync(db, ShipmentStatusName.OffersReceived, "Ton (t)", 20, [
         new OfferSpec("Test Company", 1200, OfferStateName.Accepted, true),
         new OfferSpec("Test Company", 1300, OfferStateName.Submitted),
         new OfferSpec("Test Company", 1100, OfferStateName.Rejected),
         new OfferSpec("Test Company", 1000, OfferStateName.Expired),
         new OfferSpec("Test Company", 1400, OfferStateName.Withdrawn)
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
      Assert.All(dbOffers, dbo => Assert.Equal(offers.First(o => o.Id == dbo.Id).HasWon, dbo.HasWon));
   }

   [Fact]
   public async Task CancelShipment_ConcurrentRequests_ShouldResultInConsistentState()
   {
      using var scope = factory.Services.CreateScope();
      var db = scope.ServiceProvider.GetRequiredService<CargoFlowDbContext>();

      var shipment = await ShipmentSeeder.SeedShipmentListWithUniqueStatusAsync(db, [
         new ShipmentSpec(ShipmentStatusName.Draft, "Ton (t)", 200)
      ]);

      var firstCancelRequest = _client.PostAsJsonAsync($"/api/shipments/{shipment[ShipmentStatusName.Draft].Id}/cancel", new { });
      var secondCancelRequest = _client.PostAsJsonAsync($"/api/shipments/{shipment[ShipmentStatusName.Draft].Id}/cancel", new { });
      var thirdCancelRequest = _client.PostAsJsonAsync($"/api/shipments/{shipment[ShipmentStatusName.Draft].Id}/cancel", new { });
      
      var responses = await Task.WhenAll(firstCancelRequest, secondCancelRequest, thirdCancelRequest);
      
      var successResponses = responses.Where(r => r.StatusCode == HttpStatusCode.NoContent).ToList();
      var conflictResponses = responses.Where(r => r.StatusCode == HttpStatusCode.Conflict).ToList();
      
      Assert.Single(successResponses);
      Assert.NotEmpty(conflictResponses);
      
      foreach (var conflictResponse in conflictResponses)
      {
         var problemDetail = await conflictResponse.Content.ReadFromJsonAsync<ProblemDetails>();
         Assert.NotNull(problemDetail);
         Assert.True(problemDetail.Title is "Shipment Status Conflict" or "Update Concurrency Conflict");
      }
   }

   [Fact]
   public async Task StartShipment_MissingShipments_Returns404NotFound()
   {
      using var scope = factory.Services.CreateScope();
      var db = scope.ServiceProvider.GetRequiredService<CargoFlowDbContext>();

      var maxShipmentId = await db.Shipments.MaxAsync(s => s.Id);
      var nonExistenceId = maxShipmentId + 100;

      var response = await _client.PostAsJsonAsync($"/api/shipments/{nonExistenceId}/start", new { });
      
      Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
   }

   [Fact]
   public async Task StartShipment_NotAwardedShipments_ShouldReturns409Conflict()
   {
      using var scope = factory.Services.CreateScope();
      var db = scope.ServiceProvider.GetRequiredService<CargoFlowDbContext>();

      var shipment = await ShipmentSeeder.SeedShipmentListWithUniqueStatusAsync(db, [
         new ShipmentSpec(ShipmentStatusName.Draft, "Ton (t)", 20),
         new ShipmentSpec(ShipmentStatusName.OpenForOffers, "Ton (t)", 20),
         new ShipmentSpec(ShipmentStatusName.OffersReceived, "Ton (t)", 20),
         new ShipmentSpec(ShipmentStatusName.InTransit, "Ton (t)", 20),
         new ShipmentSpec(ShipmentStatusName.Delivered, "Ton (t)", 20),
         new ShipmentSpec(ShipmentStatusName.Completed, "Ton (t)", 20),
         new ShipmentSpec(ShipmentStatusName.Cancelled, "Ton (t)", 20),
      ]);

      var ids = shipment.Values.Select(s => s.Id).ToList();
      var shipments = shipment.Values.ToList();

      foreach (var id in ids)
      {
         var response = await _client.PostAsJsonAsync($"/api/shipments/{id}/start", new { });
         Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
      }

      using var verifyScope = factory.Services.CreateScope();
      var verifyDb = verifyScope.ServiceProvider.GetRequiredService<CargoFlowDbContext>();

      var verifyShipments = await verifyDb.Shipments
         .Where(s => ids.Contains(s.Id))
         .ToListAsync();
      
      Assert.Equal(verifyShipments.Count, shipments.Count);
      Assert.All(verifyShipments, vs => Assert.Equal(vs.ShipmentStatusId, shipments
         .First(s => s.Id == vs.Id).ShipmentStatusId));
   }

   [Fact]
   public async Task StartShipment_AwardedShipments_ShouldReturns204NoContent()
   {
      using var scope = factory.Services.CreateScope();
      var db = scope.ServiceProvider.GetRequiredService<CargoFlowDbContext>();

      var (shipment, offers) = await ShipmentSeeder.SeedShipmentWithOffersAsync(db, ShipmentStatusName.Awarded,
         "Ton (t)", 20, [
            new OfferSpec("Test Company", 1200, OfferStateName.Accepted, true),
            new OfferSpec("Test Company", 1100, OfferStateName.Rejected),
            new OfferSpec("Test Company", 900, OfferStateName.Rejected),
            new OfferSpec("Test Company", 1000, OfferStateName.Rejected),
            new OfferSpec("Test Company", 1000, OfferStateName.Rejected),
         ]);

      var response = await _client.PostAsJsonAsync($"/api/shipments/{shipment.Id}/start", new { });
      
      Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

      var ids = offers.Select(o => o.Id).ToList();
      
      using var verifyScope = factory.Services.CreateScope();
      var verifyDb = verifyScope.ServiceProvider.GetRequiredService<CargoFlowDbContext>();
      var verifyShipment = await verifyDb.Shipments
         .Include(s => s.ShipmentStatus)
         .FirstOrDefaultAsync(s => s.Id == shipment.Id);
      Assert.NotNull(verifyShipment);
      Assert.Equal(ShipmentStatusName.InTransit, verifyShipment.ShipmentStatus.Name);
         
      var verifiedOffers = await verifyDb.Offers
         .Where(o => ids.Contains(o.Id))
         .ToListAsync();
      
      Assert.Equal(offers.Count, verifiedOffers.Count);
      Assert.All(verifiedOffers, vo => Assert.Equal(vo.OfferStateId, offers
         .First(o => o.Id == vo.Id).OfferStateId));
      Assert.All(verifiedOffers, vo => Assert.Equal(vo.HasWon, 
         offers.First(o => o.Id == vo.Id).HasWon));
   }

   [Fact]
   public async Task StartShipment_ConcurrentRequests_ShouldReturns409Conflict()
   {
      using var scope = factory.Services.CreateScope();
      var db = scope.ServiceProvider.GetRequiredService<CargoFlowDbContext>();
      var referenceData = scope.ServiceProvider.GetRequiredService<IReferenceData>();

      var (shipment, _) =
         await ShipmentSeeder.SeedShipmentWithOffersAsync(db, ShipmentStatusName.Awarded, "Ton (t)", 20, []);
      

      using var concurrentScope = factory.Services.CreateScope();
      var concurrentDb = concurrentScope.ServiceProvider.GetRequiredService<CargoFlowDbContext>();
      var shipmentToModify = await concurrentDb.Shipments.FirstAsync(s => s.Id == shipment.Id);
      var inTransitStatusId = await concurrentDb.ShipmentStatuses
         .Where(ss => ss.Name == ShipmentStatusName.InTransit)
         .Select(ss => ss.Id)
         .FirstAsync();

      shipmentToModify.ShipmentStatusId = inTransitStatusId;
      await concurrentDb.SaveChangesAsync();

      var result = await ShipmentEndpoints.StartShipment(shipment.Id, db, referenceData);
      
      var problemHttpResult = Assert.IsType<ProblemHttpResult>(result.Result);
      Assert.Equal(StatusCodes.Status409Conflict, problemHttpResult.StatusCode);
      Assert.Equal("Update Concurrency Exception", problemHttpResult.ProblemDetails.Title);
      Assert.Equal($"Shipment #{shipment.Id} was modified or started by another concurrent request", problemHttpResult.ProblemDetails.Detail);

   }

   [Fact]
   public async Task DeliverShipment_MissingShipments_ShouldReturns404NotFound()
   {
      using var scope = factory.Services.CreateScope();
      var db = scope.ServiceProvider.GetRequiredService<CargoFlowDbContext>();

      var maxId = await db.Shipments.MaxAsync(s => s.Id);
      var nonExistenceId = maxId + 100;

      var response = await _client.PostAsJsonAsync($"/api/shipments/{nonExistenceId}/deliver", new { });
      
      Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
   }

   [Fact]
   public async Task DeliverShipment_InvalidStatuses_ShouldReturns409Conflict()
   {
      using var scope = factory.Services.CreateScope();
      var db = scope.ServiceProvider.GetRequiredService<CargoFlowDbContext>();

      var shipments = await ShipmentSeeder.SeedShipmentListWithUniqueStatusAsync(db, [
         new ShipmentSpec(ShipmentStatusName.Draft, "Ton (t)", 20),
         new ShipmentSpec(ShipmentStatusName.OffersReceived, "Ton (t)", 20),
         new ShipmentSpec(ShipmentStatusName.OpenForOffers, "Ton (t)", 20),
         new ShipmentSpec(ShipmentStatusName.Awarded, "Ton (t)", 20),
         new ShipmentSpec(ShipmentStatusName.Delivered, "Ton (t)", 20),
         new ShipmentSpec(ShipmentStatusName.Cancelled, "Ton (t)", 20),
         new ShipmentSpec(ShipmentStatusName.Completed, "Ton (t)", 20),
      ]);

      var ids = shipments.Values.Select(s => s.Id).ToList();
      var shipmentList = shipments.Values.ToList();
      
      foreach (var id in ids)
      {
         var response = await _client.PostAsJsonAsync($"/api/shipments/{id}/deliver", new { });
         Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
      }

      using var verifyScope = factory.Services.CreateScope();
      var verifyDb = verifyScope.ServiceProvider.GetRequiredService<CargoFlowDbContext>();

      var verifyShipments = await verifyDb.Shipments
         .Where(s => ids.Contains(s.Id))
         .ToListAsync();
      
      Assert.Equal(verifyShipments.Count, shipments.Count);
      Assert.All(verifyShipments, vs => Assert.Equal(vs.ShipmentStatusId, shipmentList
         .First(s => s.Id == vs.Id).ShipmentStatusId));


   }

   [Fact]
   public async Task DeliverShipment_InTransitShipments_ShouldReturns204NoContentAndTransitionStatus()
   {
      using var scope = factory.Services.CreateScope();
      var db = scope.ServiceProvider.GetRequiredService<CargoFlowDbContext>();

      var (inTransitShipment, offers) = await ShipmentSeeder.SeedShipmentWithOffersAsync(db, ShipmentStatusName.InTransit,
         "Ton (t)", 20, [
            new OfferSpec("Test Company", 200, OfferStateName.Accepted, true),
            new OfferSpec("Test Company", 200, OfferStateName.Rejected),
            new OfferSpec("Test Company", 200, OfferStateName.Rejected),
            new OfferSpec("Test Company", 200, OfferStateName.Rejected),
            new OfferSpec("Test Company", 200, OfferStateName.Rejected),
         ]);

      var response = await _client.PostAsJsonAsync($"/api/shipments/{inTransitShipment.Id}/deliver", new { });
      
      Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

      using var verifyScope = factory.Services.CreateScope();
      var verifyDb = verifyScope.ServiceProvider.GetRequiredService<CargoFlowDbContext>();

      var verifyShipment = await verifyDb.Shipments
         .Include(s => s.Offers)
         .FirstAsync(s => s.Id == inTransitShipment.Id);
      var deliveredStatus = await verifyDb.ShipmentStatuses.FirstAsync(ss => ss.Name == ShipmentStatusName.Delivered);
      Assert.NotNull(verifyShipment);
      Assert.Equal(deliveredStatus.Id, verifyShipment.ShipmentStatusId);
      Assert.Equal(verifyShipment.Offers.Count, offers.Count);
      Assert.All(verifyShipment.Offers, vso => Assert.Equal(vso.OfferStateId, offers
         .First(o => o.Id == vso.Id).OfferStateId));
      Assert.All(verifyShipment.Offers, vso => Assert.Equal(vso.HasWon, offers
         .First(o => o.Id == vso.Id).HasWon));
   }

   [Fact]
   public async Task DeliverShipment_ConcurrentRequests_ShouldReturns409Conflict()
   {
      using var scope = factory.Services.CreateScope();
      var db = scope.ServiceProvider.GetRequiredService<CargoFlowDbContext>();
      var referenceData = scope.ServiceProvider.GetRequiredService<IReferenceData>();

      var (shipment, _) =
         await ShipmentSeeder.SeedShipmentWithOffersAsync(db, ShipmentStatusName.InTransit, "Ton (t)", 200, []);

      using var concurrentScope = factory.Services.CreateScope();
      var concurrentDb = concurrentScope.ServiceProvider.GetRequiredService<CargoFlowDbContext>();

      var concurrentShipment = await concurrentDb.Shipments.FirstAsync(s => s.Id == shipment.Id);
      var deliveredStatusId =
         await concurrentDb.ShipmentStatuses
            .Where(ss => ss.Name == ShipmentStatusName.Delivered)
            .Select(ss => ss.Id)
            .FirstAsync();
      concurrentShipment.ShipmentStatusId = deliveredStatusId;
      await concurrentDb.SaveChangesAsync();

      var result = await ShipmentEndpoints.DeliverShipment(shipment.Id, db, referenceData);

      var problemHttpResult = Assert.IsType<ProblemHttpResult>(result.Result);
      
      Assert.Equal(StatusCodes.Status409Conflict, problemHttpResult.StatusCode);
      Assert.Equal("Update Concurrency Conflict", problemHttpResult.ProblemDetails.Title);
      Assert.Equal($"Shipment {shipment.Id} was modified or delivered by another concurrent request", problemHttpResult.ProblemDetails.Detail);
   }

   [Fact]
   public async Task GetPaginatedShipment_ShouldReturns200OkWithPageNumberAndSize()
   {
      using var scope = factory.Services.CreateScope();
      var db = scope.ServiceProvider.GetRequiredService<CargoFlowDbContext>();
      var referenceData = scope.ServiceProvider.GetRequiredService<IReferenceData>();
   
      var customerId = await db.Customers.Select(c => c.Id).FirstAsync();
      var shipmentStatusId = referenceData.GetShipmentStatusId(ShipmentStatusName.Awarded);
   
      const int pageSize = 5;
      const int page = 1;
      
      var response =
         await _client.GetAsync($"/api/shipments?customer_id={customerId}&shipment_status={shipmentStatusId}&page={page}&page_size={pageSize}");
      
      testOutputHelper.WriteLine(await response.Content.ReadAsStringAsync());
   
      var dto = await response.Content.ReadFromJsonAsync<PaginatedShipmentsDto>();
   
      Assert.NotNull(dto);
      Assert.Equal(HttpStatusCode.OK, response.StatusCode);
      Assert.True(dto.Items.Count <= pageSize);
      Assert.Equal(page, dto.Page);
      Assert.Equal(pageSize, dto.PageSize);
      if (dto.Items is { Count: > 0 })
      {
         Assert.All(dto.Items, i => Assert.Equal(customerId, i.CustomerId));
         Assert.All(dto.Items, i => Assert.Equal(shipmentStatusId, i.ShipmentStatusId));
      }
   }

   [Fact]
   public async Task GetPaginatedShipment_NoFilter_ShouldReturnsAllShipments()
   {
      using var scope = factory.Services.CreateScope();
      var db = scope.ServiceProvider.GetRequiredService<CargoFlowDbContext>();

      var shipments = await db.Shipments.ToListAsync();
      var noFilterPaginatesShipments = await _client.GetAsync("/api/shipments");

      var dto = await noFilterPaginatesShipments.Content.ReadFromJsonAsync<PaginatedShipmentsDto>();

      Assert.NotNull(dto);
      Assert.Equal(shipments.Count, dto.TotalCount);
   }

   [Fact]
   public async Task GetPaginatedShipment_InvalidQueryParameters_ShouldReturns400BadRequest()
   {
      var badPageSizeResponse = await _client.GetAsync($"/api/shipments?page_size=3");
      Assert.Equal(HttpStatusCode.BadRequest, badPageSizeResponse.StatusCode);

      var badCustomerIdResponse = await _client.GetAsync($"/api/shipments?customer_id=ttt");
      Assert.Equal(HttpStatusCode.BadRequest, badCustomerIdResponse.StatusCode);
   }
   
}