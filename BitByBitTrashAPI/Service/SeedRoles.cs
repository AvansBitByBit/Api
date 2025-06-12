// namespace BitByBitTrashAPI.Service;
//
// using Microsoft.AspNetCore.Identity;
//
// public static class SeedRoles
// {
//     public static async Task SeedAsync(IServiceProvider services)
//     {
//         // try
//         // {
//             var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
//
//             string[] roles = new[] { "Beheerder", "Gebruiker", "IT", "Standaard" };
//             foreach (var role in roles)
//             {
//                 if (!await roleManager.RoleExistsAsync(role))
//                 {
//                     var result = await roleManager.CreateAsync(new IdentityRole(role));
//                     if (!result.Succeeded)
//                     {
//                         throw new InvalidOperationException(
//                             $"Failed to create role '{role}': {string.Join(", ", result.Errors.Select(e => e.Description))}");
//                     }
//                 }
//             }
//         }
//     }
// // catch (Exception ex)
//         // {
//         //     throw new InvalidOperationException($"Error seeding roles: {ex.Message}", ex);
//         // }
//     