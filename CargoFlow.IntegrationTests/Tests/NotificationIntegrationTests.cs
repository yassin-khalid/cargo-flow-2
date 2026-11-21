using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using CargoFlow.Data;
using CargoFlow.Domain;
using CargoFlow.Domain.Enums;
using CargoFlow.Dtos;
using CargoFlow.Helpers;
using CargoFlow.IntegrationTests.Helpers;
using CargoFlow.IntegrationTests.Helpers.Seeders;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualBasic;
using Xunit.Abstractions;

namespace CargoFlow.IntegrationTests.Tests;

[Collection("Database collection")]
public class NotificationIntegrationTests(LocalSqlWebApplicationFactory<Program> factory, ITestOutputHelper testOutputHelper)
{

    [Fact]
    public async Task AwardOffer_WhenSuccess_ShouldCreateCompanyNotification()
    {
        var (client, _, profileId) = await factory.CreateClientForAsync(TestData.CustomerEmail, "Customer");

        var unExpiredOffer = await factory
            .QueryDbAsync(async db => await db.Offers
                .Include(o => o.Shipment)
                .FirstOrDefaultAsync(o => o.ExpirationDate > DateTime.UtcNow
                    && o.Shipment.ShipmentStatus.Name == ShipmentStatusName.OffersReceived
                    && o.Shipment.CustomerId == profileId
                )
            );
        Assert.NotNull(unExpiredOffer);

        var response = await client.PostAsJsonAsync($"/api/shipments/{unExpiredOffer.ShipmentId}/offers/{unExpiredOffer.Id}/award", new { });
        testOutputHelper.WriteLine(await response.Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);


        var notification = await factory
            .QueryDbAsync(async db => await db.Notifications
                .OrderByDescending(n => n.CreatedAt)
                .FirstOrDefaultAsync(n => n.RecipientId == unExpiredOffer.CompanyId
                    && n.Type == NotificationType.OfferAwarded
                    && n.ShipmentId == unExpiredOffer.ShipmentId
                )
            );

        Assert.NotNull(notification);
        Assert.Equal("Offer Awarded", notification.Title);
        Assert.Equal($"Offer #{unExpiredOffer.Id} for shipment {unExpiredOffer.ShipmentId} has been awarded", notification.Message);
        Assert.Null(notification.ReadAt);
    }

    [Fact]
    public async Task SubmitOffer_WhenSuccess_ShouldCreateCustomerNotification()
    {
        var (client, _, profileId) = await factory.CreateClientForAsync(TestData.CompanyEmail, "Company");

        var shipment = await factory
            .QueryDbAsync(async db => await db.Shipments
                .FirstOrDefaultAsync(s => s.ShipmentStatus.Name == ShipmentStatusName.OpenForOffers)
            );
        Assert.NotNull(shipment);

        var response = await client.PostAsJsonAsync(
            $"/api/shipments/{shipment.Id}/offers",
            new SubmitOfferRequestBody(profileId, 100));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var notification = await factory
            .QueryDbAsync(db => db.Notifications
                .OrderByDescending(n => n.CreatedAt)
                .FirstOrDefaultAsync(n => n.ShipmentId == shipment.Id
                    && n.RecipientId == shipment.CustomerId
                    && n.Type == NotificationType.OfferSubmitted
            )
        );

        Assert.NotNull(notification);
        Assert.Equal("Offer Submitted", notification.Title);
        Assert.Equal($"New offer has been submitted successfully for shipment #{shipment.Id}", notification.Message);
        Assert.Null(notification.ReadAt);
    }

    [Fact]
    public async Task OpenForOffers_WhenSuccess_ShouldCreateCustomerNotification()
    {
        var (client, _, profileId) = await factory.CreateClientForAsync(TestData.CustomerEmail, "Customer");

        var shipment = await factory
            .QueryDbAsync(async db => await db.Shipments
                .FirstOrDefaultAsync(s => s.ShipmentStatus.Name == ShipmentStatusName.Draft
                    && s.CustomerId == profileId)
            );

        Assert.NotNull(shipment);

        var response = await client.PostAsJsonAsync($"/api/shipments/{shipment.Id}/open-for-offers", new { });
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var notification = await factory.QueryDbAsync(async db => await db.Notifications
            .OrderByDescending(n => n.CreatedAt)
            .FirstOrDefaultAsync(n => n.RecipientId == shipment.CustomerId
                && n.Type == NotificationType.ShipmentStatusChanged
                && n.ShipmentId == shipment.Id)
            );

        Assert.NotNull(notification);
        Assert.Equal("Shipment Status Changed", notification.Title);
        Assert.Equal($"Shipment #{shipment.Id} status has been changed to {ShipmentStatusName.OpenForOffers}", notification.Message);
        Assert.Null(notification.ReadAt);
    }

    [Fact]
    public async Task CancelShipment_WhenSuccess_ShouldCreateCustomerNotification()
    {
        var (client, _, profileId) = await factory.CreateClientForAsync(TestData.CustomerEmail, "Customer");

        var shipment = await factory
            .QueryDbAsync(async db => await db.Shipments
                .FirstOrDefaultAsync(s => s.ShipmentStatus.Name == ShipmentStatusName.OffersReceived
                    && s.CustomerId == profileId)
            );
        Assert.NotNull(shipment);

        var response = await client.PostAsJsonAsync($"/api/shipments/{shipment.Id}/cancel", new { });
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        testOutputHelper.WriteLine(await response.Content.ReadAsStringAsync());


        var notification = await factory.QueryDbAsync(async db => await db.Notifications
            .OrderByDescending(n => n.CreatedAt)
            .FirstOrDefaultAsync(n => n.RecipientId == shipment.CustomerId
                && n.Type == NotificationType.ShipmentStatusChanged
                && n.ShipmentId == shipment.Id
            )
        );

        Assert.NotNull(notification);
        Assert.Equal("Shipment Status Changed", notification.Title);
        Assert.Equal($"Shipment #{shipment.Id} status has been changed to {ShipmentStatusName.Cancelled}", notification.Message);
        Assert.Null(notification.ReadAt);
    }

    [Fact]
    public async Task StartShipment_WhenSuccess_ShouldCreateCustomerNotification()
    {
        var (client, _, profileId) = await factory.CreateClientForAsync(TestData.CompanyEmail, "Company");

        var offer = await factory
            .QueryDbAsync(async db => await db.Offers
                .Include(o => o.Shipment)
                .ThenInclude(s => s.ShipmentStatus)
                .FirstOrDefaultAsync(o => o.CompanyId == profileId
                    && o.OfferState.Name == OfferStateName.Accepted)
            );

        Assert.NotNull(offer);
        Assert.Equal(ShipmentStatusName.Awarded, offer.Shipment.ShipmentStatus.Name);

        var response = await client.PostAsJsonAsync($"/api/shipments/{offer.ShipmentId}/start", new { });
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var notification = await factory
        .QueryDbAsync(async db => await db.Notifications
            .OrderByDescending(n => n.CreatedAt)
            .FirstOrDefaultAsync(n => n.RecipientId == offer.Shipment.CustomerId
                && n.Type == NotificationType.ShipmentStatusChanged
                && n.ShipmentId == offer.ShipmentId
            )
        );

        Assert.NotNull(notification);
        Assert.Equal("Shipment Status Changed", notification.Title);
        Assert.Equal($"Shipment #{offer.ShipmentId} status has been changed to {ShipmentStatusName.InTransit}", notification.Message);
        Assert.Null(notification.ReadAt);

    }

    [Fact]
    public async Task DeliverShipment_WhenSuccess_ShouldCreateCustomerNotification()
    {
        var (client, _, profileId) = await factory.CreateClientForAsync(TestData.CustomerEmail, "Customer");

        var shipment = await factory
            .QueryDbAsync(async db => await db.Shipments
                .FirstOrDefaultAsync(s => s.ShipmentStatus.Name == ShipmentStatusName.InTransit
                    && s.CustomerId == profileId
                )
            );
        Assert.NotNull(shipment);

        var response = await client.PostAsJsonAsync($"/api/shipments/{shipment.Id}/deliver", new { });
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var notification = await factory
            .QueryDbAsync(async db => await db.Notifications
                .OrderByDescending(n => n.CreatedAt)
                .FirstOrDefaultAsync(n => n.RecipientId == shipment.CustomerId
                    && n.Type == NotificationType.ShipmentStatusChanged
                    && n.ShipmentId == shipment.Id
                )
           );

        Assert.NotNull(notification);
        Assert.Equal("Shipment Status Changed", notification.Title);
        Assert.Equal($"Shipment #{shipment.Id} status has been changed to {ShipmentStatusName.Delivered}", notification.Message);
        Assert.Null(notification.ReadAt);

    }

    [Fact]
    public async Task TransitionShipmentStatus_WhenSuccess_ShouldCreateCustomerNotification()
    {
        var (client, _, profileId) = await factory.CreateClientForAsync(TestData.CustomerEmail, "Customer");

        var shipment = await factory
            .QueryDbAsync(async db => await db.Shipments
                .FirstOrDefaultAsync(s => s.ShipmentStatus.Name == ShipmentStatusName.Draft
                    && s.CustomerId == profileId
                )
            );
        Assert.NotNull(shipment);

        var openStatus = await factory
            .QueryDbAsync(async db => await db.ShipmentStatuses
                .FirstOrDefaultAsync(ss => ss.Name == ShipmentStatusName.OpenForOffers
                )
            );
        Assert.NotNull(openStatus);

        var response = await client.PatchAsJsonAsync($"/api/shipments/{shipment.Id}/status", new ShipmentStatusTransitionRequest(openStatus.Id));

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var notification = await factory
            .QueryDbAsync(async db => await db.Notifications
                .FirstOrDefaultAsync(n => n.RecipientId == shipment.CustomerId
                    && n.ShipmentId == shipment.Id
                    && n.Type == NotificationType.ShipmentStatusChanged
                )
            );

        Assert.NotNull(notification);
        Assert.Equal("Shipment Status Changed", notification.Title);
        Assert.Equal($"Shipment #{shipment.Id} status has been changed to {ShipmentStatusName.OpenForOffers}", notification.Message);
        Assert.Null(notification.ReadAt);

    }

    [Fact]
    public async Task MarkNotificaitonAsRead_WhenSuccess_ShouldMakeReadAtNonNullable()
    {
        var (client, _, profileId) = await factory.CreateClientForAsync(TestData.CustomerEmail, "Customer");

        var notification = await factory
            .QueryDbAsync(async db => await db.Notifications
                .FirstOrDefaultAsync(n => n.RecipientId == profileId
                    && n.ReadAt == null
                )
            );
        Assert.NotNull(notification);

        var response = await client.PatchAsJsonAsync($"/api/notifications/{notification.Id}/read", new { });
        testOutputHelper.WriteLine(await response.Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var verifyNotification = await factory
            .QueryDbAsync(async db => await db.Notifications
                .FirstOrDefaultAsync(n => n.Id == notification.Id)
            );

        Assert.NotNull(verifyNotification);
        Assert.NotNull(verifyNotification.ReadAt);
    }

    [Fact]
    public async Task MarkNotificationsAsRead_WhenSuccess_ShouldMakeReadAtNonNullable()
    {

        var (client, _, profileId) = await factory.CreateClientForAsync(TestData.CustomerEmail, "Customer");

        var response = await client.PatchAsJsonAsync($"/api/notifications/read-all?recipient_id={profileId}", new { });
        testOutputHelper.WriteLine(await response.Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var verifyNotifications = await factory.QueryDbAsync(async db => await db.Notifications
            .Where(n => n.RecipientId == profileId)
            .ToListAsync()
        );
        Assert.NotEmpty(verifyNotifications);
        Assert.All(verifyNotifications, n => Assert.NotNull(n.ReadAt));

    }

    [Fact]
    public async Task GetNotifications_ShouldRetreiveAllFilteredNotifications()
    {
        var (client, _, profileId) = await factory.CreateClientForAsync(TestData.CustomerEmail, "Customer");

        var notifications = await factory
            .QueryDbAsync(async db => await db.Notifications
                .Where(n => n.RecipientId == profileId
                    && n.Type == NotificationType.OfferSubmitted
                )
                .ToListAsync()
            );
        Assert.NotNull(notifications);
        Assert.NotEmpty(notifications);

        var response = await client.GetAsync($"/api/notifications?notification_type=offer_submitted");
        testOutputHelper.WriteLine(await response.Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<List<NotificationDto>>();
        Assert.NotNull(result);
        Assert.NotEmpty(result);
        Assert.Equal(result.Count, notifications.Count);
        Assert.True(result.Select(n => n.Id).SequenceEqual(notifications.Select(n => n.Id)));
    }

    [Fact]
    public async Task MarkNotificationAsRead_NonExistentNotification_ShouldReturns404()
    {
        var (client, _, profileId) = await factory.CreateClientForAsync(TestData.CustomerEmail, "Customer");
        var maxNotificationId = await factory
            .QueryDbAsync(async db => await db.Notifications
                .MaxAsync(n => n.Id)
            );

        var response = await client.PatchAsJsonAsync($"/api/notifications/{maxNotificationId}/read", new { });

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
        var (client, _, profileId) = await factory.CreateClientForAsync(TestData.CustomerEmail, "Customer");
        var notification = await factory.QueryDbAsync(async db => await db.Set<UserProfile>()
                .Where(p => p.Id == profileId)
                .SelectMany(p => p.Notifications)
                .Where(n => n.ReadAt == null)
                .FirstOrDefaultAsync()
        );
        Assert.NotNull(notification);
        Assert.Null(notification.ReadAt);

        var response = await client.PatchAsJsonAsync($"/api/notifications/{notification.Id}/read", new { });
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);


        var verifyNotification = await factory
            .QueryDbAsync(async verifyDb => await verifyDb.Notifications
                .FirstOrDefaultAsync(n => n.Id == notification.Id)
            );

        Assert.NotNull(verifyNotification);
        Assert.NotNull(verifyNotification.ReadAt);
    }

    [Fact]
    public async Task GetUnreadNotificationsCount_WhenSuccess_ShouldReturnsSumOfUnreadCount200OK()
    {
        var (client, _, profileId) = await factory.CreateClientForAsync(TestData.CustomerEmail, "Customer");

        var unreadCount = await factory
            .QueryDbAsync(async db => await db.Notifications
                .Where(n => n.RecipientId == profileId && n.ReadAt == null)
                .CountAsync()
            );

        Assert.True(unreadCount > 0);

        var response = await client.GetAsync($"/api/notifications/unread-count");
        response.EnsureSuccessStatusCode();
        Assert.True(int.TryParse(await response.Content.ReadAsStringAsync(), out var count));
        Assert.Equal(count, unreadCount);
    }

    [Fact]
    public async Task GetUnreadNotificaitonCount_WhenAlreadyRead_ShouldReturnsZero()
    {
        var (client, _, profileId) = await factory.CreateClientForAsync(TestData.OtherCustomerEmail, "Customer");

        var response = await client.PatchAsJsonAsync("/api/notifications/read-all", new { });
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var notifications = await factory
            .QueryDbAsync(async db => await db.Notifications
                .Where(n => n.RecipientId == profileId
                    && n.ReadAt == null
                )
                .ToListAsync()
            );

        Assert.Empty(notifications);
    }

    // [Fact]
    // public async Task MarkNotificationAsRead_WhenOtherRecipient_ShouldReturns404()
    // {
    //     using var scope = _factory.Services.CreateScope();
    //     var db = scope.ServiceProvider.GetRequiredService<CargoFlowDbContext>();
    //     var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

    //     var notifications = await NotificationSeeder.SeedNotifications(db, userManager, 10);
    //     Assert.NotNull(notifications);
    //     Assert.Equal(10, notifications.Count);

    //     var notification = notifications[0];
    //     var otherNotification = notifications[1];

    //     var response = await _client.PatchAsJsonAsync($"/api/notifications/{otherNotification.Id}/read?recipient_type={notification.RecipientType.ToString().ToLower()}&recipient_id={notification.RecipientId}", new { });
    //     Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    // }

    // [Fact]
    // public async Task MarkAllNotificationsAsRead_WhenSuccess_OtherRecipientNotificationsShouldRemainsUnread()
    // {
    //     using var scope = _factory.Services.CreateScope();
    //     var db = scope.ServiceProvider.GetRequiredService<CargoFlowDbContext>();
    //     var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

    //     var notifications = await NotificationSeeder.SeedNotifications(db, userManager, 10);
    //     var notification = notifications.First();
    //     var notificationIds = notifications[1..].Select(n => n.Id);

    //     Assert.All(notifications, n => Assert.Null(n.ReadAt));
    //     var response = await _client.PatchAsJsonAsync($"/api/notifications/read-all?recipient_id={notification.RecipientId}&recipient_type={JsonNamingPolicy.SnakeCaseLower.ConvertName(notification.RecipientType.ToString())}", new { });

    //     _testOutputHelper.WriteLine(await response.Content.ReadAsStringAsync());

    //     Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

    //     using var verifyScope = _factory.Services.CreateScope();
    //     var verifyDb = verifyScope.ServiceProvider.GetRequiredService<CargoFlowDbContext>();

    //     var verifyNotifications = await verifyDb.Notifications
    //         .Where(n => notificationIds.Contains(n.Id))
    //         .ToListAsync();

    //     Assert.All(verifyNotifications, n => Assert.Null(n.ReadAt));
    // }


    // [Fact]
    // public async Task GetNotifications_MissingRecipientId_ShouldReturns400()
    // {
    //     using var scope = _factory.Services.CreateScope();
    //     var db = scope.ServiceProvider.GetRequiredService<CargoFlowDbContext>();
    //     var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

    //     var notifications = await NotificationSeeder.SeedNotificationsWithSharedRecipient(
    //         db,
    //         userManager,
    //         new()
    //         {
    //             [NotificationType.OfferSubmitted] = 10
    //         });
    //     var notification = notifications.First();

    //     var response = await _client.GetAsync($"/api/notifications?recipient_type={notification.RecipientType.ToString().ToLower()}&shipment_id={notification.ShipmentId}&notification_type={JsonNamingPolicy.SnakeCaseLower.ConvertName(notification.Type.ToString())}&message={notification.Message}&is_read={false}");

    //     _testOutputHelper.WriteLine(await response.Content.ReadAsStringAsync());
    //     Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    // }

    [Fact]
    public async Task GetNotifications_WhenNotSignIn_ShouldReturns401Unauthorized()
    {
        var response = await factory.CreateClient().GetAsync("/api/notifications");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ReadNotification_OtherUserNotification_ShouldReturns404NotFound()
    {
        var (_, _, profileId) = await factory.CreateClientForAsync(TestData.CustomerEmail, "Customer");
        var (client, _, otherProfileId) = await factory.CreateClientForAsync(TestData.OtherCustomerEmail, "Customer");

        var notification = await factory
            .QueryDbAsync(async db => await db.Notifications
                .FirstOrDefaultAsync(n => n.RecipientId == profileId)
            );
        Assert.NotNull(notification);

        var response = await client.PatchAsJsonAsync($"/api/notifications/{notification.Id}/read", new { });
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
