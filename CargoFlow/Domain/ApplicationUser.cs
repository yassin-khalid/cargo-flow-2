using Microsoft.AspNetCore.Identity;

namespace CargoFlow.Domain;

public class ApplicationUser : IdentityUser
{
    public UserProfile? UserProfile { get; set; }
}
