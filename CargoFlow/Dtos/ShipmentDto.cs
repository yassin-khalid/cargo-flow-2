
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;

namespace CargoFlow.Dtos;

public record ShipmentDto(
    [property: JsonPropertyName("id")]
    int Id,
    [property: JsonPropertyName("customer_id")]
    int CustomerId,
    [property: JsonPropertyName("pickup_id")]
    int PickupId,
    [property: JsonPropertyName("destination_id")]
    int DestinationId,
    [property: JsonPropertyName("weight")]
    decimal Weight,
    [property: JsonPropertyName("weight_type_id")]
    int WeightTypeId,
    [property: JsonPropertyName("shipment_status_id")]
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
    [FromQuery(Name = "customer_id")]
    public int? CustomerId { get; set; }
    [FromQuery(Name = "shipment_status")]
    public int? ShipmentStatus { get; set; }
    [FromQuery(Name = "pickup_address")]
    public int? PickupAddress { get; set; }
    [FromQuery(Name = "destination_address")]
    public int? DestinationAddress { get; set; }
    [FromQuery(Name = "page")]
    public int? Page { get; set; }
    [FromQuery(Name = "page_size")]
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
