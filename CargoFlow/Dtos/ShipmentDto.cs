
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;

namespace CargoFlow.Dtos;

public enum ShipmentSortBy
{
    Weight,
    Id
}

public enum ShipmentSortDirection
{
    Asc,
    Desc,
}
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

public record GetShipmentQuery(
    int? CustomerId,
    int? ShipmentStatus,
    int? PickupAddress,
    int? DestinationAddress,
    int Page,
    int PageSize,
    ShipmentSortBy SortBy,
    ShipmentSortDirection SortOrder);

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
    public int? PageSize { get; set; }
    [FromQuery(Name = "sort_by")]
    public string? SortBy { get; set; }
    [FromQuery(Name = "sort_order")]
    public string? SortOrder { get; set; }
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

public record ShipmentStatusTransitionRequest(
    [property: JsonPropertyName(name: "status_id")]
    int StatusId
    );
    
    public record ShipmentStatusHistoryDto(
        [property: JsonPropertyName("status")]
        string Status, 
        [property: JsonPropertyName("date_time")]
        DateTime DateTime);