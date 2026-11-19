using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

namespace Platform.ServiceDefaults.Security;

public static class PlatformAuthentication
{
    public const string SmartScheme = "Smart";   
    public const string IdentityServerScheme = "IdentityServer";
    public const string KeycloakScheme = "Keycloak";

    public static string SelectScheme(
    HttpContext context, string identityServerAuthority, string? keycloakIssuer = null)
    {
        var header = context.Request.Headers.Authorization.ToString();
        if (header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            var token = header["Bearer ".Length..].Trim();
            var handler = new JwtSecurityTokenHandler();

            if (handler.CanReadToken(token))
            {
                var issuer = handler.ReadJwtToken(token).Issuer;

                if (keycloakIssuer is not null &&
                    string.Equals(issuer, keycloakIssuer.TrimEnd('/'), StringComparison.Ordinal))
                    return KeycloakScheme;

                if (string.Equals(issuer, identityServerAuthority.TrimEnd('/'), StringComparison.Ordinal))
                    return IdentityServerScheme;
            }
        }

        return JwtBearerDefaults.AuthenticationScheme;
    }
}

public static class ScopeAuthorizationExtensions
{
    public static bool HasScope(this ClaimsPrincipal user, string scope) =>
        user.FindAll("scope").Any(c =>
            c.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries).Contains(scope, StringComparer.Ordinal));

    public static AuthorizationOptions AddScopePolicy(
        this AuthorizationOptions options,
        string policyName,
        string scope,
        string? firstPartyIssuer = null,
        string? firstPartyRole = null)
    {
        options.AddPolicy(policyName, policy => policy
            .RequireAuthenticatedUser()
            .RequireAssertion(ctx =>
                ctx.User.HasScope(scope) ||
                (firstPartyIssuer is not null
                 && ctx.User.HasClaim("iss", firstPartyIssuer)
                 && (firstPartyRole is null || ctx.User.IsInRole(firstPartyRole)))));

        return options;
    }
}