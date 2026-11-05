using CargoFlow.Dtos;

namespace CargoFlow.Extensions;

public static class ShipmentRequestExtensions
{
   public static GetShipmentQuery ToQuery(this GetPaginatedShipmentsRequestDto paginatedDto)
   {
      var sortBy = paginatedDto.SortBy switch
      {
         "id" => ShipmentSortBy.Id,
         _ => ShipmentSortBy.Weight
      };

      var sortOrder = paginatedDto.SortOrder switch
      {
         "asc" => ShipmentSortDirection.Asc,
         _ => ShipmentSortDirection.Desc
      };
      return new GetShipmentQuery(
         paginatedDto.CustomerId, 
         paginatedDto.ShipmentStatus, 
         paginatedDto.PickupAddress,
         paginatedDto.DestinationAddress, 
         paginatedDto.Page ?? 1, 
         paginatedDto.PageSize ?? 5, 
         sortBy, 
         sortOrder);
   }
}