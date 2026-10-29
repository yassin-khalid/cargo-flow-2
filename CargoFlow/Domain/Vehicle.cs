using System.ComponentModel.DataAnnotations;

namespace CargoFlow.Domain;

public class Vehicle
{
    public int Id { get; set; }
    [Required]
    [MaxLength(20, ErrorMessage = "InspectionId must have maximum 20 characters")]
    public string InspectionId { get; set; }
    [Required]
    [MaxLength(20, ErrorMessage = "InsuranceId must have maximum 20 characters")]
    public string InsuranceId { get; set; }
    public DateOnly InspectionIdExpiryDate { get; set; }
    public DateOnly InsuranceIdExpiryDate { get; set; }
    public bool IsAvailable { get; set; }
    public int CompanyId { get; set; }
    public Company Company { get; set; }
    public decimal MaxWeight { get; set; }
    public int WeightTypeId { get; set; }
    public WeightType WeightType { get; set; }
}
