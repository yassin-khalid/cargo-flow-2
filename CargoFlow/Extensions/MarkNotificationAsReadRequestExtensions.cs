using CargoFlow.Domain.Enums;
using CargoFlow.Dtos;

namespace CargoFlow.Extensions;

public static class MarkNotificationAsReadRequestExtensions
{
    public static MarkNotificationAsReadQuery ToQuery(this MarkNotificationAsReadRequestDto dto)
    {
        return new(dto.RecipientId, Enum.Parse<RecipientType>(dto.RecipientType, true));
    }
}
