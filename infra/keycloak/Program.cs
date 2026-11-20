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

    var cfg = new Config();
    var testUserPassword = cfg.RequireSecret("testUserPassword");
    var temporaryPasswords = cfg.GetBoolean("temporaryPasswords") ?? true;

    var apis = new[] { "orders-api", "products-api", "payments-api" };

    // Authorization Code Flow: Postman (confidential)
    var postman = ClientFactory
        .CreateAuthorizationCodeClient("teashop-postman", realm, isPublic: false,
            new[] { "https://oauth.pstmn.io/v1/callback" }, opts)
        .WithDefaultScopes(realm, opts)
        .WithAudiences(realm, apis, opts)
        .WithPermissionsMapper(realm, opts);

    // Authorization Code Flow: Swagger UI (public client + PKCE)
    var swagger = ClientFactory
        .CreateAuthorizationCodeClient("teashop-swagger", realm, isPublic: true,
            new[]
            {
                "https://localhost:7271/swagger/oauth2-redirect.html",   // orders-api
                "https://localhost:7208/swagger/oauth2-redirect.html",   // catalog-api
                "https://localhost:7244/swagger/oauth2-redirect.html",   // payments-api
            }, opts)
        .WithDefaultScopes(realm, opts)
        .WithAudiences(realm, apis, opts)
        .WithPermissionsMapper(realm, opts);

    // Client Credentials Flow: Aggregator
    var aggregator = ClientFactory
        .CreateClientCredentialsClient("teashop-aggregator", realm, opts)
        .WithDefaultScopes(realm, opts)
        .WithAudiences(realm, new[] { "orders-api", "products-api" }, opts);

    // Service account Aggregator отримує client-ролі reader на Orders і Products
    foreach (var rs in new[] { ordersApi, productsApi })
    {
        _ = new Keycloak.OpenId.ClientServiceAccountRole(
            $"aggregator-{rs.Name}-reader",
            new Keycloak.OpenId.ClientServiceAccountRoleArgs
            {
                RealmId = realm.Id,
                ServiceAccountUserId = aggregator.ServiceAccountUserId,
                ClientId = rs.Client.Id,
                Role = rs.Reader.Name,
            }, opts);
    }

    // Тестові користувачі: по одному на кожну роль
    TestUsers.Create(realm, realmRoles, testUserPassword, temporaryPasswords, opts);

    return new Dictionary<string, object?>
    {
        ["RealmUrl"] = Output.Format($"{url}/realms/{realm.RealmName}"),
        ["AdminConsoleUrl"] = Output.Format($"{url}/admin/master/console/#/{realm.RealmName}"),

        // порядок: postman, swagger, aggregator, orders-api, products-api, payments-api
        ["ClientIds"] = Output.All(
        postman.ClientId, swagger.ClientId, aggregator.ClientId,
        ordersApi.Client.ClientId, productsApi.Client.ClientId, paymentsApi.Client.ClientId),

        ["ClientSecrets"] = Output.All(
            postman.ClientSecret, aggregator.ClientSecret,
            ordersApi.Client.ClientSecret, productsApi.Client.ClientSecret, paymentsApi.Client.ClientSecret)
        .Apply(s => new Dictionary<string, string>
        {
            ["teashop-postman"] = s[0],
            ["teashop-aggregator"] = s[1],
            ["orders-api"] = s[2],
            ["products-api"] = s[3],
            ["payments-api"] = s[4],
        }),
    };
});