
using System.ComponentModel.DataAnnotations;

namespace CargoFlow.Dtos;

public record ShipmentDto(
    int Id,
    int CustomerId,
    int PickupId,
    int DestinationId,
    decimal Weight,
    int WeightTypeId,
    int ShipmentStatusId
);

public record CreateShipmentRequestDto(
    int CustomerId,
    int PickupId,
    int DestinationId,
    [Required]
    [Range(1, double.MaxValue, ErrorMessage = "Weight must be greater than zero")]
    decimal Weight,
    int WeightTypeId
);
