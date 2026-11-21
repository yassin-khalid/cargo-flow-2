using System.ComponentModel.DataAnnotations;
using CargoFlow.Domain.Enums;

namespace CargoFlow.Domain;

public class WeightType
{
    public int Id { get; set; }
    [Required]
    public WeightTypeName Name { get; set; }
    public List<Shipment> Shipments { get; set; } = [];
    public List<Vehicle> Vehicles { get; set; } = [];
}
