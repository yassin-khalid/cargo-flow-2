namespace CargoFlow.Domain;

public class UserProfile
{
    public int Id { get; set; }
    public string UserId { get; set; } = null!;
    public ApplicationUser User { get; set; } = null!;
    public List<Notification> Notifications { get; set; } = [];
    public List<ShipmentTrackingEvent> ShipmentTrackingEvents { get; set; } = [];
}
