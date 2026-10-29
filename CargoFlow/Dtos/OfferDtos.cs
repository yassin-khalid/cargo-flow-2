using System.ComponentModel.DataAnnotations;

namespace CargoFlow.Dtos;

public record OfferDto(
    int Id, 
    int ShipmentId, 
    int CompanyId, 
    DateTime SubmittedDate, 
    [Required]
    [Range(1, double.MaxValue, ErrorMessage = "Price must be greater than zero")]
    decimal Price, 
    int OfferStateId, 
    bool HasWon);


public record SubmitOfferRequestBody(
    int CompanyId, 
    [Required]
    [Range(1, double.MaxValue, ErrorMessage = "Price must be greater than zero")]
    decimal Price);