
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

public class GetPaginatedShipmentsRequestDto : IBindableFromHttpContext<GetPaginatedShipmentsRequestDto>
{
    public int CustomerId { get; set; }
    public int ShipmentStatus { get; set; }
    public int PickupAddress { get; set; }
    public int DestinationAddress { get; set; }

    [Range(1, double.MaxValue, ErrorMessage = "Page must be greater that zero and less than 'double.MaxValue'")]
    public int Page { get; set; } = 1;

    [AllowedValues(5, 10, 15, 20, 25, 50, 100, ErrorMessage = "Only [5, 10, 15, 20, 25, 50, 100] values are allowed")]
    public int PageSize { get; set; } = 5;


    public static ValueTask<GetPaginatedShipmentsRequestDto?> BindAsync(HttpContext context, ParameterInfo parameter)
    {
        const string customerIdKey = "customer_id";
        const string shipmentStatusKey = "shipment_status";
        const string pickupAddressKey = "pickup_address";
        const string destinationAddressKey = "destination_address";
        const string pageKey = "page";
        const string pageSizeKey = "page_size";
        
        int.TryParse(context.Request.Query[customerIdKey], out var customerId);
        int.TryParse(context.Request.Query[shipmentStatusKey], out var shipmentStatus);
        int.TryParse(context.Request.Query[pickupAddressKey], out var pickupAddress);
        int.TryParse(context.Request.Query[destinationAddressKey], out var destinationAddress) ;
        int.TryParse(context.Request.Query[pageKey], out var page);
        int.TryParse(context.Request.Query[pageSizeKey], out var pageSize);

        return ValueTask.FromResult<GetPaginatedShipmentsRequestDto?>(new GetPaginatedShipmentsRequestDto()
        {
            CustomerId = customerId,
            ShipmentStatus = shipmentStatus,
            PickupAddress = pickupAddress,
            DestinationAddress = destinationAddress,
            Page = page == 0 ? 1 : page,
            PageSize = pageSize == 0 ? 5 : pageSize
        });
    }

    public void Deconstruct(out int? customerId, out int? shipmentStatus, out int? pickupAddress,
        out int? destinationAddress, out int page, out int pageSize)
    {
        (customerId, shipmentStatus, pickupAddress, destinationAddress, page, pageSize) = (CustomerId, ShipmentStatus,
            PickupAddress, DestinationAddress, Page, PageSize);
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
