using System.ComponentModel.DataAnnotations;

namespace CargoFlow.Domain;

public class Address
{
    public int Id { get; set; }
    [Range(-90, 90, ErrorMessage = "Latitude should be within -90 to 90")]
    public decimal Latitude { get; set; }
    [Range(-180, 180, ErrorMessage = "Longitude should be within -180 to 180")]
    public decimal Longitude { get; set; }
    public List<Shipment> PickupShipments { get; set; } = [];
    public List<Shipment> DestinationShipments { get; set; } = [];
}
