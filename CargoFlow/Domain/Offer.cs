using System.ComponentModel.DataAnnotations;

namespace CargoFlow.Domain;

public class Offer
{
    public int Id { get; set; }
    public int ShipmentId { get; set; }
    public Shipment Shipment { get; set; }
    public int CompanyId { get; set; }
    public Company Company { get; set; }
    public DateTime SubmittedDate { get; set; }
    [Required]
    [Range(1, double.MaxValue, ErrorMessage = "Price must be greater than zero")]
    public decimal Price { get; set; }
    public int OfferStateId { get; set; }
    public OfferState OfferState { get; set; }
    public bool HasWon { get; set; }
    [Timestamp]
    public byte[] RowVersion { get; set; }
}
