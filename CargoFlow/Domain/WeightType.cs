using System.ComponentModel.DataAnnotations;

namespace CargoFlow.Domain;

public class WeightType
{
    public int Id { get; set; }
    [Required]
    public string Name { get; set; }
    public List<Shipment> Shipments { get; set; } = [];
    public List<Vehicle> Vehicles { get; set; } = [];
}
