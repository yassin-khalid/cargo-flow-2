using System.Security.Claims;
using CargoFlow.Data;
using CargoFlow.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CargoFlow.Helpers;

public interface IRecipientsReferenceData
{
    (RecipientType?, int?) GetRecipientData(ClaimsPrincipal user);
}

public class RecipientsReferenceData : IRecipientsReferenceData
{

    public (RecipientType?, int?) GetRecipientData(ClaimsPrincipal user)
    {

        var recipientType = user.FindFirst("user_type")?.Value;

        return recipientType switch
        {
            "Customer" when int.TryParse(user.FindFirst("customer_id")?.Value, out var customerId) => (RecipientType.Customer, customerId),
            "Company" when int.TryParse(user.FindFirst("company_id")?.Value, out var companyId) => (RecipientType.Company, companyId),
            _ => (null, null)
        };

    }
}
