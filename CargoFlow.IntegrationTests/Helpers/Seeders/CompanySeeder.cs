using CargoFlow.Data;
using CargoFlow.Domain;
using Microsoft.AspNetCore.Identity;

namespace CargoFlow.IntegrationTests.Helpers.Seeders;

public static class CompanySeeder
{
    public static async Task<List<Company>> GenerateCompaniesAsync(CargoFlowDbContext db, UserManager<ApplicationUser> userManager, int count)
    {
        List<Company> companies = [];
        var uniqueSuffix = Guid.NewGuid().ToString("N")[..5];
        using var transaction = await db.Database.BeginTransactionAsync();
        try
        {
            for (var i = 0; i < count; i++)
            {
                var subSuffix = $"{uniqueSuffix}-{i}";
                var user = new ApplicationUser
                {
                    Email = $"test-{subSuffix}@company.com",
                    UserName = $"Test Company {subSuffix}".Trim()[..Math.Min(40, $"Test Company {subSuffix}".Length)],
                };

                var result = await userManager.CreateAsync(user);
                if (!result.Succeeded)
                {
                    throw new Exception(string.Join(", ", result.Errors.Select(e => e.Description)));
                }

                var company = new Company()
                {
                    Name = $"Test Company {subSuffix}".Trim()[..Math.Min(40, $"Test Company {subSuffix}".Length)],
                    Email = $"test-{subSuffix}@company.com",
                    TaxID = $"T{i}-{subSuffix}".Trim()[..Math.Min(10, $"T{i}-{subSuffix}".Length)],
                    UserId = user.Id
                };
                companies.Add(company);
            }

            db.Companies.AddRange(companies);
            await db.SaveChangesAsync();
            await transaction.CommitAsync();

            return companies;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            throw new Exception(ex.Message);
        }
    }
}
