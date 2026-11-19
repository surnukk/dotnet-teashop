using System.Collections.Generic;
using Pulumi;
using Keycloak = Pulumi.Keycloak;
using System.Linq;

return await Deployment.RunAsync(() =>
{
    var config = new Config("keycloak");
    var url = config.Get("url") ?? "http://localhost:8080";

    // Provider з admin-credentials Keycloak
    var provider = new Keycloak.Provider("keycloak-provider", new Keycloak.ProviderArgs
    {
        Url = url,
        ClientId = config.Get("clientId") ?? "admin-cli",
        Username = config.Require("username"),
        Password = config.RequireSecret("password"),
    });

    var opts = new CustomResourceOptions { Provider = provider };

    // Realm DevRealmDrivers
    var realm = new Keycloak.Realm("dev-realm", new Keycloak.RealmArgs
    {
        RealmName = "DevRealmDrivers",
        Enabled = true,
        DisplayName = "TeaShop Dev Realm",

        RegistrationAllowed = true,
        ResetPasswordAllowed = true,
        RememberMe = true,
        LoginWithEmailAllowed = true,

        AccessTokenLifespan = "15m",

        LoginTheme = "keycloak",
        AccountTheme = "keycloak.v3",
        AdminTheme = "keycloak.v2",
        EmailTheme = "keycloak",
    }, opts);

    // Realm-ролі (централізовані): admin, manager, user, guest
    var realmRoles = PermissionMatrix.Roles.ToDictionary(
        roleName => roleName,
        roleName => new Keycloak.Role($"realm-role-{roleName}", new Keycloak.RoleArgs
        {
            RealmId = realm.Id,
            Name = roleName,
            Description = $"TeaShop realm role: {roleName}",
        }, opts));

    // Resource Servers: orders-api, products-api, payments-api
    var ordersApi = ResourceServerFactory.Create("orders-api", "Orders API", realm, realmRoles, opts);
    var productsApi = ResourceServerFactory.Create("products-api", "Products API", realm, realmRoles, opts);
    var paymentsApi = ResourceServerFactory.Create("payments-api", "Payments API", realm, realmRoles, opts);

    return new Dictionary<string, object?>
    {
        ["RealmUrl"] = Output.Format($"{url}/realms/{realm.RealmName}"),
        ["AdminConsoleUrl"] = Output.Format($"{url}/admin/master/console/#/{realm.RealmName}"),
    };
});