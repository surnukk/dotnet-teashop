using System.Collections.Generic;
using System.Linq;
using Pulumi;
using Keycloak = Pulumi.Keycloak;

public sealed record TestUser(string Username, string FirstName, string LastName, string Role);

public static class TestUsers
{
    public static readonly TestUser[] All =
    {
        new("alice.admin",  "Alice", "Admin",   PermissionMatrix.Admin),
        new("john.manager", "John",  "Manager", PermissionMatrix.Manager),
        new("bob.user",     "Bob",   "User",    PermissionMatrix.User),
        new("greta.guest",  "Greta", "Guest",   PermissionMatrix.Guest),
    };

    public static void Create(
        Keycloak.Realm realm,
        IReadOnlyDictionary<string, Keycloak.Role> realmRoles,
        Input<string> password,
        bool temporaryPassword,
        CustomResourceOptions opts)
    {
        // Permissions кожної ролі лежать в атрибуті групи; mapper віддає їх у токен
        var groups = PermissionMatrix.Roles.ToDictionary(
            role => role,
            role => new Keycloak.Group($"group-{role}", new Keycloak.GroupArgs
            {
                RealmId = realm.Id,
                Name = $"{role}-permissions",
                Attributes = { ["authorization"] = PermissionMatrix.BuildAuthorizationAttribute(role) },
            }, opts));

        foreach (var u in All)
        {
            var user = new Keycloak.User($"user-{u.Username}", new Keycloak.UserArgs
            {
                RealmId = realm.Id,
                Username = u.Username,
                Enabled = true,
                Email = $"{u.Username}@teashop.local",
                EmailVerified = true,
                FirstName = u.FirstName,
                LastName = u.LastName,
                InitialPassword = new Keycloak.Inputs.UserInitialPasswordArgs
                {
                    Value = password,
                    Temporary = temporaryPassword,
                },
            }, opts);

            _ = new Keycloak.UserRoles($"user-roles-{u.Username}", new Keycloak.UserRolesArgs
            {
                RealmId = realm.Id,
                UserId = user.Id,
                RoleIds = { realmRoles[u.Role].Id },
            }, opts);

            _ = new Keycloak.UserGroups($"user-groups-{u.Username}", new Keycloak.UserGroupsArgs
            {
                RealmId = realm.Id,
                UserId = user.Id,
                GroupIds = { groups[u.Role].Id },
            }, opts);
        }
    }
}