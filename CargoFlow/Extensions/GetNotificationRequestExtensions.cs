using CargoFlow.Domain.Enums;
using CargoFlow.Dtos;

namespace CargoFlow.Extensions;

public static class GetNotificationRequestExtensions
{
    private static T? ToEnum<T>(string? value) where T : struct, Enum
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        if (Enum.TryParse<T>(value, out var result))
        {
            return result;
        }
        else
        {
            return null;
        }
    }
    public static GetNotificationQuery ToQuery(this GetNotificationRequestDto dto)
    {

        return new GetNotificationQuery(
         Enum.Parse<RecipientType>(dto.RecipientType, true),
            dto.RecipientId,
            dto.ShipmentId,
            ToEnum<NotificationType>(dto.NotificationType),
            dto.Title,
            dto.Message,
            dto.IsRead);
    }
}
