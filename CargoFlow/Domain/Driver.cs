using System.ComponentModel.DataAnnotations;

namespace CargoFlow.Domain;

public class Driver
{
    public int Id { get; set; }
    [Required]
    [MaxLength(25, ErrorMessage = "Name must have 25 character maximum")]
    public string Name { get; set; }
    public DateOnly DateOfBirth { get; set; }
    [Required]
    [MaxLength(10, ErrorMessage = "Phone must have 10 digits maximum")]
    public string Phone { get; set; }
    public int CompanyId { get; set; }
    public Company Company { get; set; }
}
