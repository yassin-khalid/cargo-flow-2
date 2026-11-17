namespace CargoFlow.Domain;

public class UserProfile
{
    public int Id { get; set; }
    public string UserId { get; set; }
    public ApplicationUser User { get; set; }
}
