using System.ComponentModel.DataAnnotations;
using CargoFlow.Domain.Enums;

namespace CargoFlow.Domain;

public class ShipmentStatus
{
    public int Id { get; set; }
    [Required]
    [MaxLength(20, ErrorMessage = "Name must have maximum 20 characters")]
    public ShipmentStatusName Name { get; set; }
    public List<ShipmentStatusHistory> ShipmentStatusHistories { get; set; } = [];
    public List<Shipment> Shipments { get; set; } = [];
}
