using System.ComponentModel.DataAnnotations;
using CargoFlow.Domain.Enums;

namespace CargoFlow.Domain;

public class ShipmentTrackingEvent
{
    public int Id { get; set; }
    public ShipmentEventType EventType { get; set; }

    public int ShipmentId { get; set; }
    public Shipment Shipment { get; set; } = null!;

    [Required]
    [MaxLength(25, ErrorMessage = "Description must have 25 characters maximum")]
    public string Description { get; set; } = null!;
    public string? Location { get; set; }

    [Range(-180, 180, ErrorMessage = "Longitude should be within -180 to 180")]
    public decimal? Longitude { get; set; }

    [Range(-90, 90, ErrorMessage = "Latitude should be within -90 to 90")]
    public decimal? Latitude { get; set; }

    public DateTimeOffset OccurredAtUtc { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }

    public int CreatedByUserId { get; set; }
    public UserProfile CreatedByUser { get; set; } = null!;
}
