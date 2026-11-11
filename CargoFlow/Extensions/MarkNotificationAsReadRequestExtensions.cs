using CargoFlow.Domain.Enums;
using CargoFlow.Dtos;

namespace CargoFlow.Extensions;

public static class MarkNotificationAsReadRequestExtensions
{
    public static MarkNotificationAsReadQuery ToQuery(this MarkNotificationAsReadRequestDto dto)
    {
        var recipientType = dto.RecipientType switch
        {
            "company" => RecipientType.Company,
            _ => RecipientType.Customer
        };
        return new(dto.RecipientId, recipientType);
    }
}
