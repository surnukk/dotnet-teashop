using Duende.IdentityServer.Models;

namespace Platform.IdentityServer;

public static class Config
{
    public static IEnumerable<IdentityResource> IdentityResources => new IdentityResource[]
    {
        new IdentityResources.OpenId(),
        new IdentityResources.Profile()
    };

    public static IEnumerable<ApiScope> ApiScopes => new[]
    {
        new ApiScope("orders_api", "Orders API (повний доступ)"),
        new ApiScope("products_api", "Products API (повний доступ)"),
        new ApiScope("orders.read", "Читання замовлень"),
        new ApiScope("orders.write", "Створення та зміна замовлень"),
        new ApiScope("products.read", "Читання товарів"),
        new ApiScope("products.write", "Зміна каталогу товарів")
    };

    public static IEnumerable<ApiResource> ApiResources => new[]
    {
        new ApiResource("orders_api", "Orders API")
        {
            Scopes = { "orders_api", "orders.read", "orders.write" }
        },
        new ApiResource("products_api", "Products API")
        {
            Scopes = { "products_api", "products.read", "products.write" }
        }
    };

    public static IEnumerable<Client> Clients => new[]
    {
        new Client
        {
            ClientId = "postman",
            ClientName = "Postman",
            AllowedGrantTypes = GrantTypes.Code,
            ClientSecrets = { new Secret("secret".Sha256()) },
            RedirectUris = { "https://oauth.pstmn.io/v1/callback" },
            AllowedScopes =
            {
                "openid", "profile",
                "orders_api", "products_api",
                "orders.read", "orders.write", "products.read", "products.write"
            },
            AllowOfflineAccess = true
        },

        new Client
        {
            ClientId = "aggregator_service",
            ClientName = "Aggregator Service",
            AllowedGrantTypes = GrantTypes.ClientCredentials,
            ClientSecrets = { new Secret("aggregator_secret".Sha256()) },
            AllowedScopes = { "orders_api", "products_api", "orders.read", "products.read" }
        },

        new Client
        {
            ClientId = "swagger",
            ClientName = "Swagger UI",
            AllowedGrantTypes = GrantTypes.Code,
            RequirePkce = true,
            RequireClientSecret = false,
            RedirectUris =
            {
                "https://localhost:5001/swagger/oauth2-redirect.html",  
                "https://localhost:7271/swagger/oauth2-redirect.html"  
            },
            AllowedCorsOrigins =             
            {
                "https://localhost:5001",
                "https://localhost:7271"
            },
            AllowedScopes = { "openid", "profile", "orders_api", "orders.read", "orders.write" }
        }
    };
}