using CargoFlow.Data;
using CargoFlow.Domain;

namespace CargoFlow.IntegrationTests.Helpers;

public static class CompanySeeder
{
    public static async Task<List<Company>> GenerateCompaniesAsync(CargoFlowDbContext db, int count)
    {
        List<Company> companies = [];
        var uniqueSuffix = Guid.NewGuid().ToString("N")[..5];
        for (var i = 0; i < count; i++)
        {
            var subSuffix = $"{uniqueSuffix}-{i}";
            var company = new Company()
            {
                Name = $"Test Company {subSuffix}".Trim()[..Math.Min(40, $"Test Company {subSuffix}".Length)],
                Email = $"test-{subSuffix}@company.com",
                TaxID = $"T{i}-{subSuffix}".Trim()[..Math.Min(10, $"T{i}-{subSuffix}".Length)]
            };
            companies.Add(company);
        }

        db.Companies.AddRange(companies);
        await db.SaveChangesAsync();
        
        return companies;
    }
}