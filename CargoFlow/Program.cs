using System.Text;
using System.Text.Json;
using CargoFlow.Data;
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
builder.Services.AddDbContext<CargoFlowDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("CargoFlow")));
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower);
builder.Services.AddSingleton<IReferenceData, ReferenceData>();
builder.Services.AddScoped<IRecipientsReferenceData, RecipientsReferenceData>();
builder.Services.AddScoped<IOfferExpirationService, OfferExpirationService>();
builder.Services.AddHostedService<OfferExpirationWorker>();
builder.Services.AddSingleton<INotificationService, NotificationService>();
builder.Services.AddValidation();

builder.Services.AddIdentity<IdentityUser, IdentityRole>(options =>
    {
        options.User.RequireUniqueEmail = true;
    }
)
    .AddEntityFrameworkStores<CargoFlowDbContext>()
    .AddDefaultTokenProviders();

var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = Encoding.UTF8.GetBytes(jwtSettings["Secret"]!);

builder.Services.AddAuthentication(options =>
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
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

// if (builder.Environment.IsDevelopment())
// {
//     builder.Configuration.AddUserSecrets<Program>();
// }

var app = builder.Build();

var group = app.MapGroup("/api");

group
    .MapGroup("/weight-types")
    .MapWeightTypeEndpoints();
group
    .MapGroup("/customers")
    .MapCustomerEndpoints();

group
    .MapGroup("/shipments")
    .MapShipmentEndpoints()
    .MapOfferEndpoints();

group
    .MapGroup("/notifications")
    .MapNotificationsEndpoints()
    .RequireAuthorization();

group.MapGroup("/auth")
    .MapAuthEndpoints();


app.UseExceptionHandler(errorApp => errorApp.Run(async context =>
{
    var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;
    if (exception is InvalidOperationException)
    {
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        await context.Response.WriteAsJsonAsync(new
        {
            statusCode = StatusCodes.Status500InternalServerError,
            title = "Server Configuration Error",
            detail = exception.Message
        });
    }
}));

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
        logger.LogError(ex, "An error occur while migrating database or seeding roles in the database");
        throw;
    }
}

app.Run();
