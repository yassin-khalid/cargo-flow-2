using CargoFlow.Data;
using CargoFlow.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CargoFlow.Helpers;

public interface IRecipientsReferenceData
{
    (RecipientType, int) GetRecipinetDataFromUserId(string userId);
}

public class RecipientsReferenceData : IRecipientsReferenceData
{
    private readonly Dictionary<string, int> _companyUserId;
    private readonly Dictionary<string, int> _customerUserId;

    public RecipientsReferenceData(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CargoFlowDbContext>();

        _companyUserId = db.Companies
            .AsNoTracking()
            .Where(c => c.CompanyUserId != null)
            .ToDictionary(c => c.CompanyUserId!, c => c.Id);

        _customerUserId = db.Customers
            .AsNoTracking()
            .Where(c => c.CustomerUserId != null)
            .ToDictionary(c => c.CustomerUserId!, c => c.Id);
    }

    public (RecipientType, int) GetRecipinetDataFromUserId(string userId)
    {
        if (_customerUserId.TryGetValue(userId, out var customerId))
        {
            return (RecipientType.Customer, customerId);
        }
        if (_companyUserId.TryGetValue(userId, out var companyId))
        {
            return (RecipientType.Company, companyId);
        }
        throw new ArgumentException("Invalid userId", nameof(userId));

    }
}
