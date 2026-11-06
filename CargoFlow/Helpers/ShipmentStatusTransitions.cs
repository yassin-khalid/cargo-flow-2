using CargoFlow.Domain.Enums;

namespace CargoFlow.Helpers;

public static class ShipmentStatusTransitions
{
   public static List<ShipmentStatusName> GetAllowedTransitions(ShipmentStatusName statusName)
   {
      return statusName switch
      {
         ShipmentStatusName.Draft => [ShipmentStatusName.OpenForOffers, ShipmentStatusName.Cancelled],
         ShipmentStatusName.OpenForOffers =>
            [ShipmentStatusName.OffersReceived, ShipmentStatusName.Awarded, ShipmentStatusName.Cancelled],
         ShipmentStatusName.OffersReceived => [ShipmentStatusName.Awarded, ShipmentStatusName.Cancelled],
         ShipmentStatusName.Awarded => [ShipmentStatusName.InTransit],
         ShipmentStatusName.InTransit => [ShipmentStatusName.Delivered],
         ShipmentStatusName.Delivered => [ShipmentStatusName.Completed],
         _ => []
      };
   }
   
}