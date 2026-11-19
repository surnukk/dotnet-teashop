using System.Collections.Generic;
using System.Linq;
using Pulumi;
using Keycloak = Pulumi.Keycloak;

public sealed record ResourceServer(
    string Name,
    Keycloak.OpenId.Client Client,
    Keycloak.Role Reader,
    Keycloak.Role Writer,
    Keycloak.Role Full);

public static class ResourceServerFactory
{
    public static ResourceServer Create(
        string apiName,
        string displayName,
        Keycloak.Realm realm,
        IReadOnlyDictionary<string, Keycloak.Role> realmRoles,
        CustomResourceOptions opts)
    {
        // 1. Resource Server = confidential client з увімкненим Authorization
        var client = new Keycloak.OpenId.Client(apiName, new Keycloak.OpenId.ClientArgs
        {
            RealmId = realm.Id,
            ClientId = apiName,
            Name = displayName,
            Enabled = true,
            AccessType = "CONFIDENTIAL",
            StandardFlowEnabled = false,
            ServiceAccountsEnabled = true,          // обов'язково для Authorization
            Authorization = new Keycloak.OpenId.Inputs.ClientAuthorizationArgs
            {
                PolicyEnforcementMode = "ENFORCING",
                DecisionStrategy = "UNANIMOUS",
                AllowRemoteResourceManagement = true,
                KeepDefaults = false,               // без default resource/policy/permission
            },
        }, opts);

        // 2. Scopes (таблиця 6)
        var scopes = PermissionMatrix.Scopes[apiName].ToDictionary(
            scope => scope,
            scope => new Keycloak.OpenId.ClientAuthorizationScope(
                $"{apiName}-scope-{scope.Replace(':', '-')}",
                new Keycloak.OpenId.ClientAuthorizationScopeArgs
                {
                    RealmId = realm.Id,
                    ResourceServerId = client.ResourceServerId,
                    Name = scope,
                }, opts));

        // 3. Ресурс з ім'ям API: саме воно потрапляє в токен як "rsname"
        var resourceScopes = new InputList<string>();
        foreach (var scope in scopes.Values) resourceScopes.Add(scope.Name);

        var resource = new Keycloak.OpenId.ClientAuthorizationResource(
            $"{apiName}-resource",
            new Keycloak.OpenId.ClientAuthorizationResourceArgs
            {
                RealmId = realm.Id,
                ResourceServerId = client.ResourceServerId,
                Name = apiName,
                DisplayName = displayName,
                Scopes = resourceScopes,
            }, opts);

        // 4. Permission Policies: роль -> політика -> scope-based permission
        foreach (var (roleName, roleScopes) in PermissionMatrix.RoleScopes[apiName])
        {
            var policy = new Keycloak.OpenId.ClientRolePolicy(
                $"{apiName}-policy-{roleName}",
                new Keycloak.OpenId.ClientRolePolicyArgs
                {
                    RealmId = realm.Id,
                    ResourceServerId = client.ResourceServerId,
                    Name = $"{roleName}-role-policy",
                    DecisionStrategy = "UNANIMOUS",
                    Logic = "POSITIVE",
                    Type = "role",
                    Roles =
                    {
                        new Keycloak.OpenId.Inputs.ClientRolePolicyRoleArgs
                        {
                            Id = realmRoles[roleName].Id,
                            Required = true,
                        },
                    },
                }, opts);

            var permissionScopes = new InputList<string>();
            foreach (var scopeName in roleScopes) permissionScopes.Add(scopes[scopeName].Id);

            _ = new Keycloak.OpenId.ClientAuthorizationPermission(
                $"{apiName}-permission-{roleName}",
                new Keycloak.OpenId.ClientAuthorizationPermissionArgs
                {
                    RealmId = realm.Id,
                    ResourceServerId = client.ResourceServerId,
                    Name = $"{apiName}:{roleName}",
                    Description = $"Role '{roleName}' on {apiName}",
                    DecisionStrategy = "UNANIMOUS",
                    Type = "scope",
                    Resources = { resource.Id },
                    Scopes = permissionScopes,
                    Policies = { policy.Id },
                }, opts);
        }

        // 5. Client-level ролі для service accounts + composite-роль
        var reader = new Keycloak.Role($"{apiName}-role-reader", new Keycloak.RoleArgs
        {
            RealmId = realm.Id,
            ClientId = client.Id,
            Name = "reader",
            Description = $"Read access to {apiName} (service accounts)",
        }, opts);

        var writer = new Keycloak.Role($"{apiName}-role-writer", new Keycloak.RoleArgs
        {
            RealmId = realm.Id,
            ClientId = client.Id,
            Name = "writer",
            Description = $"Write access to {apiName} (service accounts)",
        }, opts);

        var full = new Keycloak.Role($"{apiName}-role-full", new Keycloak.RoleArgs
        {
            RealmId = realm.Id,
            ClientId = client.Id,
            Name = "full",
            Description = $"Composite: reader + writer on {apiName}",
            CompositeRoles = { reader.Id, writer.Id },
        }, opts);

        return new ResourceServer(apiName, client, reader, writer, full);
    }
}