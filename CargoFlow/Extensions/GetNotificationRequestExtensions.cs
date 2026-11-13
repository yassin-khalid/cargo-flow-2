using System.Text.Json;
using CargoFlow.Domain.Enums;
using CargoFlow.Dtos;
using CargoFlow.Helpers;

namespace CargoFlow.Extensions;

public static class GetNotificationRequestExtensions
{
    public static GetNotificationQuery ToQuery(this GetNotificationRequestDto dto)
    {
        return new GetNotificationQuery(
            EnumCache<RecipientType>.ToEnumRequired(dto.RecipientType),
            dto.RecipientId,
            dto.ShipmentId,
            EnumCache<NotificationType>.ToEnum(dto.NotificationType),
            dto.Title,
            dto.Message,
            dto.IsRead);
    }
}
