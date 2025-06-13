// namespace BitByBitTrashAPI.Service;
using Microsoft.AspNetCore.Identity;

public static class SeedRoles
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        string[] roles = new[] { "Beheerder", "Gebruiker", "IT", "Standaard" };
        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));
        }
    }
}