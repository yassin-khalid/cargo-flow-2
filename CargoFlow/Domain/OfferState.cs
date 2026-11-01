using System.ComponentModel.DataAnnotations;
using CargoFlow.Domain.Enums;

namespace CargoFlow.Domain;

public class OfferState
{
    public int Id { get; set; }
    [Required]
    [MaxLength(20, ErrorMessage = "Name must have 20 characters maximum")]
    public OfferStateName Name { get; set; }
    public List<Offer> Offers { get; set; } = [];
}
