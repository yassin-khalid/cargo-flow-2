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
builder.Services.AddSingleton<IRecipientsReferenceData, RecipientsReferenceData>();
builder.Services.AddScoped<IOfferExpirationService, OfferExpirationService>();
builder.Services.AddHostedService<OfferExpirationWorker>();
builder.Services.AddSingleton<INotificationService, NotificationService>();
builder.Services.AddValidation();

builder.Services.AddIdentity<IdentityUser, IdentityRole>()
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

app.Lifetime.ApplicationStarted.Register(async () =>
    {
        using var scope = app.Services.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        string[] roles = ["Customer", "Company"];

        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }
    });

app.Run();
