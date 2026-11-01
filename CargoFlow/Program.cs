using System.Text.Json;
using CargoFlow.Data;
using CargoFlow.Endpoints;
using CargoFlow.Helpers;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder();
builder.Services.AddDbContext<CargoFlowDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("CargoFlow")));
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower);
builder.Services.AddSingleton<IReferenceData, ReferenceData>();
builder.Services.AddValidation();

if (builder.Environment.IsDevelopment())
{
    builder.Configuration.AddUserSecrets<Program>();
}

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

app.Run();
