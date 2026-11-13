using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CargoFlow.Data;
using CargoFlow.Domain;
using CargoFlow.Domain.Enums;
using CargoFlow.Dtos;
using CargoFlow.Helpers;
using CargoFlow.IntegrationTests.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;

namespace CargoFlow.IntegrationTests;

[Collection("Database collection")]
public class NotificationIntegrationTests(LocalSqlWebApplicationFactory<Program> factory, ITestOutputHelper testOutputHelper)
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

        var offerId = offers[0].Id;
        var response = await _client.PostAsJsonAsync($"/api/shipments/{shipment.Id}/offers/{offerId}/award", new { });
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        using var verifyScope = factory.Services.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<CargoFlowDbContext>();

        var notification = await verifyDb.Notifications
            .FirstOrDefaultAsync(n => n.RecipientId == offers[0].CompanyId
                && n.Type == NotificationType.OfferAwarded
                && n.RecipientType == RecipientType.Company
                && n.ShipmentId == shipment.Id
            );

        Assert.NotNull(notification);
        Assert.Equal("Offer Awarded", notification.Title);
        Assert.Equal($"Offer #{offerId} for shipment {shipment.Id} has been awarded", notification.Message);
        Assert.Null(notification.ReadAt);
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
            .FirstOrDefaultAsync(n => n.ShipmentId == shipment.Id
                && n.RecipientId == shipment.CustomerId
                && n.RecipientType == RecipientType.Customer
                && n.Type == NotificationType.OfferSubmitted
            );

        Assert.NotNull(notification);
        Assert.Equal("Offer Submitted", notification.Title);
        Assert.Equal($"New offer has been submitted successfully for shipment #{shipment.Id}", notification.Message);
        Assert.Null(notification.ReadAt);

    }

    [Fact]
    public async Task OpenForOffers_WhenSuccess_ShouldCreateCustomerNotification()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CargoFlowDbContext>();

        var shipmentWithStatus = await ShipmentSeeder.SeedShipmentListWithUniqueStatusAsync(db, [
            new(ShipmentStatusName.Draft, "Ton (t)", 100)
        ]);
        var shipment = shipmentWithStatus.Values.First();

        var response = await _client.PostAsJsonAsync($"/api/shipments/{shipment.Id}/open-for-offers", new { });
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        using var verifyScope = factory.Services.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<CargoFlowDbContext>();

        var notification = await verifyDb.Notifications
        .FirstOrDefaultAsync(n => n.RecipientId == shipment.CustomerId
            && n.RecipientType == RecipientType.Customer
            && n.Type == NotificationType.ShipmentStatusChanged
            && n.ShipmentId == shipment.Id);

        Assert.NotNull(notification);
        Assert.Equal("Shipment Status Changed", notification.Title);
        Assert.Equal($"Shipment #{shipment.Id} status has been changed to {ShipmentStatusName.OpenForOffers}", notification.Message);
        Assert.Null(notification.ReadAt);

    }

    [Fact]
    public async Task CancelShipment_WhenSuccess_ShouldCreateCustomerNotification()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CargoFlowDbContext>();

        var shipmentWithStatus = await ShipmentSeeder.SeedShipmentListWithUniqueStatusAsync(db, [
            new(ShipmentStatusName.Draft, "Ton (t)", 100)
        ]);
        var shipment = shipmentWithStatus.Values.First();

        var response = await _client.PostAsJsonAsync($"/api/shipments/{shipment.Id}/cancel", new { });
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        using var verifyScope = factory.Services.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<CargoFlowDbContext>();

        var notification = await verifyDb.Notifications
        .FirstOrDefaultAsync(n => n.RecipientId == shipment.CustomerId
            && n.RecipientType == RecipientType.Customer
            && n.Type == NotificationType.ShipmentStatusChanged
            && n.ShipmentId == shipment.Id);

        Assert.NotNull(notification);
        Assert.Equal("Shipment Status Changed", notification.Title);
        Assert.Equal($"Shipment #{shipment.Id} status has been changed to {ShipmentStatusName.Cancelled}", notification.Message);
        Assert.Null(notification.ReadAt);
    }

    [Fact]
    public async Task StartShipment_WhenSuccess_ShouldCreateCustomerNotification()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CargoFlowDbContext>();

        var shipmentWithStatus = await ShipmentSeeder.SeedShipmentListWithUniqueStatusAsync(db, [
            new(ShipmentStatusName.Awarded, "Ton (t)", 100)
        ]);
        var shipment = shipmentWithStatus.Values.First();

        var response = await _client.PostAsJsonAsync($"/api/shipments/{shipment.Id}/start", new { });
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        using var verifyScope = factory.Services.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<CargoFlowDbContext>();

        var notification = await verifyDb.Notifications
        .FirstOrDefaultAsync(n => n.RecipientId == shipment.CustomerId
            && n.RecipientType == RecipientType.Customer
            && n.Type == NotificationType.ShipmentStatusChanged
            && n.ShipmentId == shipment.Id);

        Assert.NotNull(notification);
        Assert.Equal("Shipment Status Changed", notification.Title);
        Assert.Equal($"Shipment #{shipment.Id} status has been changed to {ShipmentStatusName.InTransit}", notification.Message);
        Assert.Null(notification.ReadAt);
    }

    [Fact]
    public async Task DeliverShipment_WhenSuccess_ShouldCreateCustomerNotification()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CargoFlowDbContext>();

        var shipmentWithStatus = await ShipmentSeeder.SeedShipmentListWithUniqueStatusAsync(db, [
            new(ShipmentStatusName.InTransit, "Ton (t)", 100)
        ]);
        var shipment = shipmentWithStatus.Values.First();

        var response = await _client.PostAsJsonAsync($"/api/shipments/{shipment.Id}/deliver", new { });
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        using var verifyScope = factory.Services.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<CargoFlowDbContext>();

        var notification = await verifyDb.Notifications
        .FirstOrDefaultAsync(n => n.RecipientId == shipment.CustomerId
            && n.RecipientType == RecipientType.Customer
            && n.Type == NotificationType.ShipmentStatusChanged
            && n.ShipmentId == shipment.Id);

        Assert.NotNull(notification);
        Assert.Equal("Shipment Status Changed", notification.Title);
        Assert.Equal($"Shipment #{shipment.Id} status has been changed to {ShipmentStatusName.Delivered}", notification.Message);
        Assert.Null(notification.ReadAt);
    }

    [Fact]
    public async Task TransitionShipmentStatus_WhenSuccess_ShouldCreateCustomerNotification()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CargoFlowDbContext>();
        var referenceData = scope.ServiceProvider.GetRequiredService<IReferenceData>();

        var draftShipment = await ShipmentSeeder.SeedShipmentListWithUniqueStatusAsync(db, [
            new(ShipmentStatusName.Draft, "Ton (t)", 100)
        ]);

        var openForOfferStatusId = referenceData.GetShipmentStatusId(ShipmentStatusName.OpenForOffers);

        var shipment = draftShipment.Values.First();
        var response = await _client.PatchAsJsonAsync($"/api/shipments/{shipment.Id}/status", new ShipmentStatusTransitionRequest(openForOfferStatusId));

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        using var verifyScope = factory.Services.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<CargoFlowDbContext>();

        var notification = await verifyDb.Notifications
            .FirstOrDefaultAsync(n => n.RecipientId == shipment.CustomerId
                && n.ShipmentId == shipment.Id
                && n.RecipientType == RecipientType.Customer
                && n.Type == NotificationType.ShipmentStatusChanged
        );

        Assert.NotNull(notification);
        Assert.Equal("Shipment Status Changed", notification.Title);
        Assert.Equal($"Shipment #{shipment.Id} status has been changed to {ShipmentStatusName.OpenForOffers}", notification.Message);
        Assert.Null(notification.ReadAt);
    }

    [Fact]
    public async Task MarkNotificaitonAsRead_WhenSuccess_ShouldMakeReadAtNonNullable()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CargoFlowDbContext>();

        var notifications = await NotificationSeeder.SeedNotifications(db, 10);
        var notification = notifications.First();

        Assert.Null(notification.ReadAt);
        var response = await _client.PatchAsJsonAsync($"/api/notifications/{notification.Id}/read?recipient_id={notification.RecipientId}&recipient_type={JsonNamingPolicy.SnakeCaseLower.ConvertName(notification.RecipientType.ToString())}", new { });

        testOutputHelper.WriteLine(await response.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        using var verifyScope = factory.Services.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<CargoFlowDbContext>();

        var verifyNotification = await verifyDb.Notifications
            .FirstOrDefaultAsync(n => n.Id == notification.Id);

        Assert.NotNull(verifyNotification);
        Assert.NotNull(verifyNotification.ReadAt);
    }

    [Fact]
    public async Task MarkNotificationsAsRead_WhenSuccess_ShouldMakeReadAtNonNullable()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CargoFlowDbContext>();

        var notifications = await NotificationSeeder.SeedNotificationsWithSharedRecipient(db, 10);
        var notification = notifications.First();
        var notificationIds = notifications.Select(n => n.Id);

        Assert.All(notifications, n => Assert.Null(n.ReadAt));
        var response = await _client.PatchAsJsonAsync($"/api/notifications/read-all?recipient_id={notification.RecipientId}&recipient_type={JsonNamingPolicy.SnakeCaseLower.ConvertName(notification.RecipientType.ToString())}", new { });

        testOutputHelper.WriteLine(await response.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        using var verifyScope = factory.Services.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<CargoFlowDbContext>();

        var verifyNotifications = await verifyDb.Notifications
            .Where(n => notificationIds.Contains(n.Id))
            .ToListAsync();

        Assert.All(verifyNotifications, n => Assert.NotNull(n.ReadAt));
    }

    [Fact]
    public async Task GetNotifications_ShouldRetreiveAllFilteredNotifications()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CargoFlowDbContext>();

        var notifications = await NotificationSeeder.SeedNotificationsWithSharedRecipient(db, 10);
        var notification = notifications.First();

        var response = await _client.GetAsync($"/api/notifications?recipient_type={notification.RecipientType.ToString().ToLower()}&recipient_id={notification.RecipientId}&shipment_id={notification.ShipmentId}&notification_type={JsonNamingPolicy.SnakeCaseLower.ConvertName(notification.Type.ToString())}&message={notification.Message}&is_read={false}");

        testOutputHelper.WriteLine(await response.Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<List<NotificationDto>>();

        Assert.NotNull(result);
        Assert.NotEmpty(result);

        using var verifyScope = factory.Services.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<CargoFlowDbContext>();

        var verifyNotifications = await verifyDb.Notifications
            .Where(n => n.RecipientId == notification.RecipientId
                && n.RecipientType == notification.RecipientType
                && n.ShipmentId == notification.ShipmentId
                && n.Type == notification.Type
                && n.Message == notification.Message
                && n.ReadAt == null
            )
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync();

        Assert.NotNull(verifyNotifications);

        var verifyNotificationDtos = verifyNotifications
            .Select(n => new NotificationDto(n.Id,
                        n.RecipientType.ToString(),
                        n.RecipientId,
                        n.ShipmentId,
                        n.Type.ToString(),
                        n.Title,
                        n.Message,
                        n.CreatedAt,
                        n.ReadAt != null))
            .ToList();

        Assert.Equal(result.Count, verifyNotificationDtos.Count);
        Assert.True(result.Select(n => n.Id).SequenceEqual(verifyNotificationDtos.Select(n => n.Id)));

    }

    [Fact]
    public async Task MarkNotificationAsRead_NonExistentNotification_ShouldReturns404()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CargoFlowDbContext>();
        var maxNotificationId = await db.Notifications.MaxAsync(n => n.Id);

        var response = await _client.PatchAsJsonAsync($"/api/notifications/{maxNotificationId}/read?recipient_type={RecipientType.Company.ToString().ToLower()}&recipient_id={1}", new { });

        var result = await response.Content.ReadFromJsonAsync<ProblemDetails>();

        Assert.NotNull(result);
        testOutputHelper.WriteLine(result.Detail);
        Assert.Equal(StatusCodes.Status404NotFound, result.Status);
        Assert.Equal("Notification Not Found", result.Title);
        Assert.Equal($"Notification #{maxNotificationId} does not exist", result.Detail);
    }

    [Fact]
    public async Task MarkNotificationAsRead_WhenSuccess_ShouldMakeReadAtNonNullable()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CargoFlowDbContext>();

        var notifications = await NotificationSeeder.SeedNotifications(db, 10);
        var notification = notifications.FirstOrDefault();
        Assert.NotNull(notification);
        Assert.Null(notification.ReadAt);

        var response = await _client.PatchAsJsonAsync($"/api/notifications/{notification.Id}/read?recipient_type={notification.RecipientType.ToString().ToLower()}&recipient_id={notification.RecipientId}", new { });
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        using var verifyScope = factory.Services.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<CargoFlowDbContext>();

        var verifyNotification = await verifyDb.Notifications.FirstOrDefaultAsync(n => n.Id == notification.Id);

        Assert.NotNull(verifyNotification);
        Assert.NotNull(verifyNotification.ReadAt);
    }

    [Fact]
    public async Task GetUnreadNotificationsCount_WhenSuccess_ShouldReturnsSumOfUnreadCount200OK()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CargoFlowDbContext>();

        var notifications = await NotificationSeeder.SeedNotificationsWithSharedRecipient(db, 10, NotificationReadStatus.ReadUnread);
        var recipientId = notifications[0].RecipientId;
        var recipientTypeString = notifications[0].RecipientType.ToString().ToLower();
        var unreadCount = notifications.Count(n => n.ReadAt == null);
        var readCount = notifications.Count(n => n.ReadAt != null);
        Assert.Equal(notifications.Count, unreadCount + readCount);

        var response = await _client.GetAsync($"/api/notifications/unread-count?recipient_type={recipientTypeString}&recipient_id={recipientId}");
        response.EnsureSuccessStatusCode();
        Assert.True(int.TryParse(await response.Content.ReadAsStringAsync(), out var count));
        Assert.Equal(count, unreadCount);
    }

    [Fact]
    public async Task GetUnreadNotificaitonCount_WhenAlreadyReadOrUnmatchedRecipient_ShouldReturnsZero()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CargoFlowDbContext>();

        var notifications = await NotificationSeeder.SeedNotificationsWithSharedRecipient(db, 10, NotificationReadStatus.Read);
        Assert.NotNull(notifications);
        Assert.Equal(10, notifications.Count);

        var recipientId = notifications[0].RecipientId;
        var recipientTypeString = notifications[0].RecipientType.ToString().ToLower();

        var response = await _client.GetAsync($"/api/notifications/unread-count?recipient_type={recipientTypeString}&recipient_id={recipientId}");
        response.EnsureSuccessStatusCode();

        Assert.True(int.TryParse(await response.Content.ReadAsStringAsync(), out var count));
        Assert.Equal(0, count);
    }

    [Fact]
    public async Task MarkNotificationAsRead_WhenOtherRecipient_ShouldReturns404()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CargoFlowDbContext>();

        var notifications = await NotificationSeeder.SeedNotifications(db, 10);
        Assert.NotNull(notifications);
        Assert.Equal(10, notifications.Count);

        var notification = notifications[0];
        var otherNotification = notifications[1];

        var response = await _client.PatchAsJsonAsync($"/api/notifications/{otherNotification.Id}/read?recipient_type={notification.RecipientType.ToString().ToLower()}&recipient_id={notification.RecipientId}", new { });
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task MarkAllNotificationsAsRead_WhenSuccess_OtherRecipientNotificationsShouldRemainsUnread()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CargoFlowDbContext>();

        var notifications = await NotificationSeeder.SeedNotifications(db, 10);
        var notification = notifications.First();
        var notificationIds = notifications[1..].Select(n => n.Id);

        Assert.All(notifications, n => Assert.Null(n.ReadAt));
        var response = await _client.PatchAsJsonAsync($"/api/notifications/read-all?recipient_id={notification.RecipientId}&recipient_type={JsonNamingPolicy.SnakeCaseLower.ConvertName(notification.RecipientType.ToString())}", new { });

        testOutputHelper.WriteLine(await response.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        using var verifyScope = factory.Services.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<CargoFlowDbContext>();

        var verifyNotifications = await verifyDb.Notifications
            .Where(n => notificationIds.Contains(n.Id))
            .ToListAsync();

        Assert.All(verifyNotifications, n => Assert.Null(n.ReadAt));
    }


    [Fact]
    public async Task GetNotifications_MissingRecipientId_ShouldReturns400()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CargoFlowDbContext>();

        var notifications = await NotificationSeeder.SeedNotificationsWithSharedRecipient(db, 10);
        var notification = notifications.First();

        var response = await _client.GetAsync($"/api/notifications?recipient_type={notification.RecipientType.ToString().ToLower()}&shipment_id={notification.ShipmentId}&notification_type={JsonNamingPolicy.SnakeCaseLower.ConvertName(notification.Type.ToString())}&message={notification.Message}&is_read={false}");

        testOutputHelper.WriteLine(await response.Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

}
