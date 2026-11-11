using System.ComponentModel.DataAnnotations;
using CargoFlow.Domain.Enums;

namespace CargoFlow.Domain;

public class Notification
{
    public int Id { get; set; }
    public RecipientType RecipientType { get; set; }
    public int RecipientId { get; set; }
    public int ShipmentId { get; set; }
    public Shipment Shipment { get; set; }
    public NotificationType Type { get; set; }
    [Required]
    [MaxLength(25, ErrorMessage = "Title must have 25 characters maximum")]
    public string Title { get; set; }
    [Required]
    [MaxLength(100, ErrorMessage = "Message must have 100 characters maximum")]
    public string Message { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ReadAt { get; set; }
}
