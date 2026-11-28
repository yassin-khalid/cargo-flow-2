using System.Text;
using System.Text.Json;
using CargoFlow.Data;
using CargoFlow.Domain;
using CargoFlow.Endpoints;
using CargoFlow.Helpers;
using CargoFlow.Services;
using CargoFlow.Workers;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder();
builder.Configuration.AddUserSecrets<Program>(optional: true);
builder
    .Services.AddOptions<FileStorageOptions>()
    .Bind(builder.Configuration.GetSection(FileStorageOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();
builder.Services.AddDbContext<CargoFlowDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("CargoFlow"))
);
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;
    options.SerializerOptions.RespectRequiredConstructorParameters = true;
});
builder.Services.AddSingleton<IReferenceData, ReferenceData>();
builder.Services.AddScoped<IRecipientsReferenceData, RecipientsReferenceData>();
builder.Services.AddScoped<IPendingDocumentDeletionService, PendingDocumentDeletionService>();
builder.Services.AddScoped<IOfferExpirationService, OfferExpirationService>();
builder.Services.AddScoped<IFileStorageService, FileStorageService>();
builder.Services.AddHostedService<OfferExpirationWorker>();
builder.Services.AddHostedService<PendingDocumentDeletionWorker>();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<INotificationService, NotificationService>();
builder.Services.AddValidation();

builder
    .Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        options.User.RequireUniqueEmail = true;
    })
    .AddEntityFrameworkStores<CargoFlowDbContext>()
    .AddDefaultTokenProviders();

var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = Encoding.UTF8.GetBytes(jwtSettings["Secret"]!);

builder
    .Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters()
        {
            ValidateAudience = true,
            ValidateIssuer = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ValidAudience = jwtSettings["Audience"],
            ValidIssuer = jwtSettings["Issuer"],
            IssuerSigningKey = new SymmetricSecurityKey(secretKey),
            ClockSkew = TimeSpan.Zero,
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

var group = app.MapGroup("/api").RequireAuthorization();

group.MapGroup("/weight-types").MapWeightTypeEndpoints();
group.MapGroup("/customers").MapCustomerEndpoints();

group
    .MapGroup("/shipments")
    .MapShipmentEndpoints()
    .MapOfferEndpoints()
    .MapShipmentDocumentEndpoint()
    .MapTrackingEventEndpoints();

group.MapGroup("/notifications").MapNotificationsEndpoints();

group.MapGroup("/auth").MapAuthEndpoints().AllowAnonymous();

app.UseExceptionHandler(errorApp =>
    errorApp.Run(async context =>
    {
        var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;
        if (exception is InvalidOperationException)
        {
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await context.Response.WriteAsJsonAsync(
                new
                {
                    statusCode = StatusCodes.Status500InternalServerError,
                    title = "Server Configuration Error",
                    detail = exception.Message,
                }
            );
        }
    })
);

app.UseAuthentication();
app.UseAuthorization();

using (var scope = app.Services.CreateScope())
{
    var serviceProvider = scope.ServiceProvider;
    try
    {
        var context = serviceProvider.GetRequiredService<CargoFlowDbContext>();
        await context.Database.MigrateAsync();

        await IdentitySeeder.SeedRolesAsync(serviceProvider);
    }
    catch (Exception ex)
    {
        var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogError(
            ex,
            "An error occur while migrating database or seeding roles in the database"
        );
        throw;
    }
}

app.Run();
