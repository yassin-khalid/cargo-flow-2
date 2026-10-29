using System.ComponentModel.DataAnnotations;

namespace CargoFlow.Domain;

public class Customer
{
    public int Id { get; set; }
    [Required]
    [MaxLength(25, ErrorMessage = "Name must have 25 character maximum")]
    public string Name { get; set; }
    [Required]
    [MaxLength(10, ErrorMessage = "NationalId must have 10 characters maximum")]
    public string NationalId { get; set; }

    public List<Shipment> Shipments { get; set; } = [];
}
