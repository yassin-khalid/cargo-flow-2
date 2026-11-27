using System.Security.Claims;

namespace CargoFlow.Helpers;

public interface IRecipientsReferenceData
{
    int? GetRecipientId(ClaimsPrincipal user);
}

public class RecipientsReferenceData : IRecipientsReferenceData
{
    public int? GetRecipientId(ClaimsPrincipal user)
    {
        var recipientType = user.FindFirst("user_type")?.Value;

        return recipientType switch
        {
            "Customer"
                when int.TryParse(user.FindFirst("customer_id")?.Value, out var customerId) =>
                customerId,
            "Company" when int.TryParse(user.FindFirst("company_id")?.Value, out var companyId) =>
                companyId,
            _ => null,
        };
    }
}
