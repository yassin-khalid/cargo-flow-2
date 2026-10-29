using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

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
    [property: JsonPropertyName("company_id")]
    int CompanyId, 
    [Required]
    [Range(1, double.MaxValue, ErrorMessage = "Price must be greater than zero")]
    [property: JsonPropertyName("price")]
    decimal Price);