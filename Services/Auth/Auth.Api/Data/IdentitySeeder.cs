using Auth.Api.Common;
using Microsoft.AspNetCore.Identity;

namespace Auth.Api.Data;

public static class IdentitySeeder
{
    public static async Task SeedAsync(IServiceProvider services, IConfiguration config)
    {
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

        foreach (var role in new[] { Roles.User, Roles.Admin })
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));
        }

        var section = config.GetSection("Seed:Admin");
        var userName = section["UserName"];
        var email = section["Email"];
        var password = section["Password"];

        if (string.IsNullOrWhiteSpace(userName) || string.IsNullOrWhiteSpace(email) ||
            string.IsNullOrWhiteSpace(password))
            return;

        if (await userManager.FindByNameAsync(userName) is not null)
            return;

        var admin = new ApplicationUser { UserName = userName, Email = email, EmailConfirmed = true };
        var result = await userManager.CreateAsync(admin, password);
        if (result.Succeeded)
            await userManager.AddToRolesAsync(admin, new[] { Roles.User, Roles.Admin });
    }
}