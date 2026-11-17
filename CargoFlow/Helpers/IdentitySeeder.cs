using Microsoft.AspNetCore.Identity;

namespace CargoFlow.Helpers;

public static class IdentitySeeder
{
    public static async Task SeedRolesAsync(IServiceProvider serviceProvider)
    {
        var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        List<string> roles = ["Customer", "Company"];
        foreach (var roleName in roles)
        {

            if (!await roleManager.RoleExistsAsync(roleName))
            {
                var result = await roleManager.CreateAsync(new IdentityRole(roleName));
                if (!result.Succeeded)
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    throw new InvalidOperationException($"Failed to seed role {roleName}: {errors}");
                }
            }
        }
    }
}
