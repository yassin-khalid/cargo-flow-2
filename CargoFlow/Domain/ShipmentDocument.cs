using System.ComponentModel.DataAnnotations;
using CargoFlow.Domain.Enums;

namespace CargoFlow.Domain;

public class ShipmentDocument
{
    public int Id { get; set; }

    public int ShipmentId { get; set; }
    public Shipment Shipment { get; set; }

    [Required]
    public required string OriginalFileName { get; set; }
    [Required]
    public required string StorageKey { get; set; }
    [Required]
    public required string ContentType { get; set; }
    [Required]
    public required long FileSize { get; set; }

    public string? DocumentType { get; set; }

    public int UploadedById { get; set; }
    public UserProfile UploadedBy { get; set; }

    public DateTime UploadedAt { get; set; }

    public DocumentDeletionStatus DeletionStatus { get; set; } = DocumentDeletionStatus.Active;
    public DateTime? DeletionRequestedAtUtc { get; set; }
}
