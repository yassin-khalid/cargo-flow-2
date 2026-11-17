using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace CargoFlow.Domain;

public class Company
{
    public int Id { get; set; }
    [Required]
    [MaxLength(40, ErrorMessage = "Name must have maximum 40 characters")]
    public string Name { get; set; }
    [Required]
    [EmailAddress]
    public string Email { get; set; }
    [Required]
    [MaxLength(15, ErrorMessage = "TaxId must have maximum 15 characters")]
    public string TaxID { get; set; }

    public string? CompanyUserId { get; set; }
    public IdentityUser? CompanyUser { get; set; }

    public List<Driver> Drivers { get; set; } = [];
    public List<Offer> Offers { get; set; } = [];
    public List<Vehicle> Vehicles { get; set; } = [];

}
