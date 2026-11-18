using System.Security.Claims;
using Duende.IdentityServer.EntityFramework.DbContexts;
using Duende.IdentityServer.EntityFramework.Mappers;
using IdentityServerHost.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Platform.IdentityServer.Data;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        await services.GetRequiredService<PersistedGrantDbContext>().Database.MigrateAsync();
        await services.GetRequiredService<ApplicationDbContext>().Database.MigrateAsync();

        var config = services.GetRequiredService<ConfigurationDbContext>();
        await config.Database.MigrateAsync();

        await SeedConfigurationAsync(config);
        await SeedUsersAsync(services.GetRequiredService<UserManager<ApplicationUser>>());
    }

    private static async Task SeedConfigurationAsync(ConfigurationDbContext db)
    {
        foreach (var resource in Config.IdentityResources)
            if (!await db.IdentityResources.AnyAsync(x => x.Name == resource.Name))
                db.IdentityResources.Add(resource.ToEntity());

        foreach (var scope in Config.ApiScopes)
            if (!await db.ApiScopes.AnyAsync(x => x.Name == scope.Name))
                db.ApiScopes.Add(scope.ToEntity());

        foreach (var resource in Config.ApiResources)
            if (!await db.ApiResources.AnyAsync(x => x.Name == resource.Name))
                db.ApiResources.Add(resource.ToEntity());

        foreach (var client in Config.Clients)
            if (!await db.Clients.AnyAsync(x => x.ClientId == client.ClientId))
                db.Clients.Add(client.ToEntity());

        await db.SaveChangesAsync();
    }

    private static async Task SeedUsersAsync(UserManager<ApplicationUser> users)
    {
        await EnsureUserAsync(users, "alice", "alice@teashop.local", "Pass123$", "Alice Smith");
        await EnsureUserAsync(users, "bob", "bob@teashop.local", "Pass123$", "Bob Smith");
    }

    private static async Task EnsureUserAsync(
        UserManager<ApplicationUser> users, string userName, string email, string password, string fullName)
    {
        if (await users.FindByNameAsync(userName) is not null)
            return;

        var user = new ApplicationUser { UserName = userName, Email = email, EmailConfirmed = true };

        var created = await users.CreateAsync(user, password);
        if (!created.Succeeded)
            throw new InvalidOperationException(
                $"Cannot create user '{userName}': {string.Join("; ", created.Errors.Select(e => e.Description))}");

        var names = fullName.Split(' ', 2);
        var claims = await users.AddClaimsAsync(user, new[]
        {
            new Claim("name", fullName),
            new Claim("given_name", names[0]),
            new Claim("family_name", names.Length > 1 ? names[1] : string.Empty)
        });

        if (!claims.Succeeded)
            throw new InvalidOperationException(
                $"Cannot add claims for '{userName}': {string.Join("; ", claims.Errors.Select(e => e.Description))}");
    }
}