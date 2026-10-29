using System.ComponentModel.DataAnnotations;

namespace CargoFlow.Domain;

public class ShipmentStatus
{
    public int Id { get; set; }
    [Required]
    [MaxLength(20, ErrorMessage = "Name must have maximum 20 characters")]
    public string Name { get; set; }
    public List<ShipmentStatusHistory> ShipmentStatusHistories { get; set; } = [];
    public List<Shipment> Shipments { get; set; } = [];
}
