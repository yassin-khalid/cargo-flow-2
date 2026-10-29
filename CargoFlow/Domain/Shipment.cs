using System.ComponentModel.DataAnnotations;

namespace CargoFlow.Domain;

public class Shipment
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public Customer Customer { get; set; }
    public int PickupId { get; set; }
    public Address Pickup { get; set; }
    public int DestinationId { get; set; }
    public Address Destination { get; set; }
    [Required]
    [Range(1, double.MaxValue, ErrorMessage = "Weight must be greater than zero")]
    public decimal Weight { get; set; }
    public int WeightTypeId { get; set; }
    public WeightType WeightType { get; set; }
    public int ShipmentStatusId { get; set; }
    public ShipmentStatus ShipmentStatus { get; set; }
    public List<Offer> Offers { get; set; } = [];
    public List<ShipmentStatusHistory> ShipmentStatusHistories { get; set; } = [];
    [Timestamp]
    public byte[] RowVersion { get; set; }
}
