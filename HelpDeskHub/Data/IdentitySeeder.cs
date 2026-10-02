using Microsoft.AspNetCore.Identity;

namespace HelpDeskHub.Data;

public static class IdentitySeeder
{
    public static async Task SeedRolesAsync(
        IServiceProvider serviceProvider)
    {
        var roleManager =
            serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        string[] roles =
        {
            "Employee",
            "SupportAgent",
            "Administrator"
        };

        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(
                    new IdentityRole(role));
            }
        }
    }

    public static async Task SeedAdminAsync(
        IServiceProvider serviceProvider)
    {
        var userManager =
            serviceProvider.GetRequiredService<UserManager<IdentityUser>>();

        const string adminEmail = "admin@example.com";
        const string adminPassword = "Admin@123456";

        var adminUser = await userManager.FindByEmailAsync(adminEmail);

        if (adminUser == null)
        {
            adminUser = new IdentityUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true
            };

            var createResult = await userManager.CreateAsync(
                adminUser,
                adminPassword);

            if (createResult.Succeeded)
            {
                await userManager.AddToRoleAsync(
                    adminUser,
                    "Administrator");
            }
        }
    }
}