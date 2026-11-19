using System.Collections.Generic;
using Pulumi;
using Keycloak = Pulumi.Keycloak;

public static class ClientFactory
{
    // Authorization Code Flow: Postman (confidential) і Swagger (public + PKCE)
    public static Keycloak.OpenId.Client CreateAuthorizationCodeClient(
        string clientId, Keycloak.Realm realm, bool isPublic,
        string[] redirectUris, CustomResourceOptions opts)
    {
        var args = new Keycloak.OpenId.ClientArgs
        {
            RealmId = realm.Id,
            ClientId = clientId,
            Name = clientId,
            Enabled = true,
            AccessType = isPublic ? "PUBLIC" : "CONFIDENTIAL",
            StandardFlowEnabled = true,
            DirectAccessGrantsEnabled = !isPublic,   // лише dev: щоб скриптом перевірити матрицю
            ValidRedirectUris = redirectUris,
            WebOrigins = { "+" },
        };
        if (isPublic) args.PkceCodeChallengeMethod = "S256";   // PKCE обов'язковий для public client

        return new Keycloak.OpenId.Client(clientId, args, opts);
    }

    // Client Credentials Flow (service-to-service): Aggregator
    public static Keycloak.OpenId.Client CreateClientCredentialsClient(
        string clientId, Keycloak.Realm realm, CustomResourceOptions opts)
        => new(clientId, new Keycloak.OpenId.ClientArgs
        {
            RealmId = realm.Id,
            ClientId = clientId,
            Name = clientId,
            Enabled = true,
            AccessType = "CONFIDENTIAL",
            StandardFlowEnabled = false,
            ServiceAccountsEnabled = true,
        }, opts);
}

public static class ClientExtensions
{
    private static readonly string[] StandardScopes =
        { "basic", "acr", "profile", "email", "roles", "web-origins" };

    // Default scopes (basic потрібен, щоб у токені був sub)
    public static Keycloak.OpenId.Client WithDefaultScopes(
        this Keycloak.OpenId.Client client, Keycloak.Realm realm, CustomResourceOptions opts)
    {
        _ = new Keycloak.OpenId.ClientDefaultScopes(
            $"{client.GetResourceName()}-default-scopes",
            new Keycloak.OpenId.ClientDefaultScopesArgs
            {
                RealmId = realm.Id,
                ClientId = client.Id,
                DefaultScopes = StandardScopes,
            }, opts);
        return client;
    }

    // Audience mappers: які API можуть приймати токен
    public static Keycloak.OpenId.Client WithAudiences(
        this Keycloak.OpenId.Client client, Keycloak.Realm realm,
        IEnumerable<string> audiences, CustomResourceOptions opts)
    {
        foreach (var audience in audiences)
        {
            _ = new Keycloak.OpenId.AudienceProtocolMapper(
                $"{client.GetResourceName()}-aud-{audience}",
                new Keycloak.OpenId.AudienceProtocolMapperArgs
                {
                    RealmId = realm.Id,
                    ClientId = client.Id,
                    Name = $"audience-{audience}",
                    IncludedClientAudience = audience,
                    AddToAccessToken = true,
                    AddToIdToken = false,
                }, opts);
        }
        return client;
    }

    // Mapper claim "authorization": бере JSON з атрибута групи ролі користувача
    public static Keycloak.OpenId.Client WithPermissionsMapper(
        this Keycloak.OpenId.Client client, Keycloak.Realm realm, CustomResourceOptions opts)
    {
        _ = new Keycloak.OpenId.UserAttributeProtocolMapper(
            $"{client.GetResourceName()}-permissions",
            new Keycloak.OpenId.UserAttributeProtocolMapperArgs
            {
                RealmId = realm.Id,
                ClientId = client.Id,
                Name = "authorization-permissions",
                UserAttribute = "authorization",
                ClaimName = "authorization.permissions",
                ClaimValueType = "JSON",
                Multivalued = true,
                AddToAccessToken = true,
                AddToIdToken = false,
                AddToUserinfo = false,
            }, opts);
        return client;
    }
}