
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Text.Json.Serialization;

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

public record PaginatedShipmentsDto(
    [property: JsonPropertyName("items")]
    List<ShipmentDto> Items, 
    [property: JsonPropertyName("page")]
    int Page, 
    [property: JsonPropertyName("page_size")]
    int PageSize, 
    [property: JsonPropertyName("total_count")]
    int TotalCount);

public class GetPaginatedShipmentsRequestDto 
{
    public int? CustomerId { get; set; }
    public int? ShipmentStatus { get; set; }
    public int? PickupAddress { get; set; }
    public int? DestinationAddress { get; set; }
    public int? Page { get; set; }
    public int? PageSize { get; set; } = 5;

    public void Deconstruct(out int? customerId, out int? shipmentStatus, out int? pickupAddress,
        out int? destinationAddress, out int page, out int pageSize)
    {
        (customerId, shipmentStatus, pickupAddress, destinationAddress, page, pageSize) = (CustomerId, ShipmentStatus,
            PickupAddress, DestinationAddress, Page ?? 1, PageSize ?? 5);
    }
}


public record CreateShipmentRequestDto(
    int CustomerId,
    int PickupId,
    int DestinationId,
    [Required]
    [Range(1, double.MaxValue, ErrorMessage = "Weight must be greater than zero")]
    decimal Weight,
    int WeightTypeId
);
