using System.Net;
using System.Net.Http.Json;
using CargoFlow.Data;
using CargoFlow.Dtos;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;

namespace CargoFlow.IntegrationTests.Tests;

[Collection("Database collection")]
public class AuthIntegrationTests(LocalSqlWebApplicationFactory<Program> factory, ITestOutputHelper testOutputHelper)
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task RegisterCustomer_WhenSuccess_ShouldCreateCustomerAndReturns200()
    {

        var response = await _client.PostAsJsonAsync("/api/auth/register/customer", new RegisterCustomerRequestDto("test@test111.com", "YasKhal1125@C", "test test", "123456789"));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<RegisterResponseDto>();
        Assert.NotNull(result);
        Assert.Equal("User created successfully", result.Message);


        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CargoFlowDbContext>();

        var customer = await db.Customers
            .FirstOrDefaultAsync(c => c.Name == "test test");

        Assert.NotNull(customer);
    }

    [Fact]
    public async Task RegisterCompany_WhenSuccess_ShouldCreateCustomerAndReturns200()
    {

        var response = await _client.PostAsJsonAsync("/api/auth/register/company", new RegisterCompanyRequestDto("test@test112.com", "YasKhal1125@C", "test test", "123456789"));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<RegisterResponseDto>();
        Assert.NotNull(result);
        Assert.Equal("User created successfully", result.Message);


        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CargoFlowDbContext>();

        var company = await db.Companies
            .FirstOrDefaultAsync(c => c.Email == "test@test112.com");

        Assert.NotNull(company);
    }

    [Fact]
    public async Task RegisterCompany_InvalidPassword_ShouldReturns400BadRequest()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/register/company", new RegisterCompanyRequestDto("test@test112.com", "ttt", "test test", "123456789"));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(result);
        Assert.Equal("User Creation Issue", result.Title);
    }
}
