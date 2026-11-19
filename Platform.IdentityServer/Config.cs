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
        new ApiScope("payments_api", "Payments API (повний доступ)"),
        new ApiScope("orders.read", "Читання замовлень"),
        new ApiScope("orders.write", "Створення та зміна замовлень"),
        new ApiScope("products.read", "Читання товарів"),
        new ApiScope("products.write", "Зміна каталогу товарів"),
        new ApiScope("payments.read", "Читання платежів"),
        new ApiScope("payments.write", "Створення та повернення платежів"),
        new ApiScope("reviews_api", "Reviews API (повний доступ)"),
        new ApiScope("reviews.read", "Читання відгуків")
    };

    public static IEnumerable<ApiResource> ApiResources => new[]
    {
        new ApiResource("orders_api", "Orders API")
        {
            Scopes = { "orders_api", "orders.read", "orders.write" },
            UserClaims = { "email", "name" }  
        },
        new ApiResource("products_api", "Products API")
        {
            Scopes = { "products_api", "products.read", "products.write" }
        },
        new ApiResource("reviews_api", "Reviews API")
        {
            Scopes = { "reviews_api", "reviews.read" }
        },
        new ApiResource("payments_api", "Payments API")
        {
            Scopes = { "payments_api", "payments.read", "payments.write" }
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
                "orders_api", "products_api", "payments_api",
                "orders.read", "orders.write",
                "products.read", "products.write",
                "payments.read", "payments.write"
            },
            AllowOfflineAccess = true
        },

        new Client
        {
            ClientId = "aggregator_service",
            ClientName = "Aggregator Service",
            AllowedGrantTypes = GrantTypes.ClientCredentials,
            ClientSecrets = { new Secret("aggregator_secret".Sha256()) },
            AllowedScopes = { "orders_api", "products_api", "orders.read", "products.read", "reviews.read" }
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
            AllowedCorsOrigins = { "https://localhost:5001", "https://localhost:7271" },
            AllowedScopes = { "openid", "profile", "orders_api", "orders.read", "orders.write" }
        }
    };
}