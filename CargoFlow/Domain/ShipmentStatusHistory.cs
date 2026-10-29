namespace CargoFlow.Domain;

public class ShipmentStatusHistory
{
    public int Id { get; set; }
    public int ShipmentStatusId { get; set; }
    public ShipmentStatus ShipmentStatus { get; set; }
    public int ShipmentId { get; set; }
    public Shipment Shipment { get; set; }
    public DateTime DateTime { get; set; } = DateTime.UtcNow;
}
