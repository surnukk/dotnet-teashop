using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace Platform.ServiceDefaults.Security;

public sealed class PermissionRequirement(string permission, string? legacyPolicy = null)
    : IAuthorizationRequirement
{
    public string Permission { get; } = permission;
    public string? LegacyPolicy { get; } = legacyPolicy;
}

public sealed class PermissionAuthorizationHandler(IServiceProvider services)
    : AuthorizationHandler<PermissionRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context, PermissionRequirement requirement)
    {
        var user = context.User;

        // 1. Токен Keycloak: authorization.permissions[].scopes[] — перевірка ЛОКАЛЬНО
        if (HasPermission(user, requirement.Permission))
        {
            context.Succeed(requirement);
            return;
        }

        // 2. Токени Ч.1–2 (Auth Service, IdentityServer) не мають claim "authorization":
        //    для них діє попередня scope-based політика
        if (requirement.LegacyPolicy is not null && !user.HasClaim(c => c.Type == "authorization"))
        {
            var authorization = services.GetRequiredService<IAuthorizationService>();
            var result = await authorization.AuthorizeAsync(user, requirement.LegacyPolicy);
            if (result.Succeeded) context.Succeed(requirement);
        }
    }

    private static bool HasPermission(ClaimsPrincipal user, string permission)
    {
        foreach (var claim in user.FindAll("authorization"))
        {
            try
            {
                using var doc = JsonDocument.Parse(claim.Value);
                if (!doc.RootElement.TryGetProperty("permissions", out var permissions)) continue;

                foreach (var p in permissions.EnumerateArray())
                {
                    if (p.TryGetProperty("scopes", out var scopes) &&
                        scopes.EnumerateArray().Any(s => s.GetString() == permission))
                        return true;
                }
            }
            catch (JsonException) { /* некоректний claim — ігноруємо */ }
        }
        return false;
    }
}

// [RequirePermission("orders:create")] = [Authorize(Policy = "orders:create")]
public sealed class RequirePermissionAttribute(string permission) : AuthorizeAttribute(permission)
{
    public string Permission { get; } = permission;
}

public static class PermissionAuthorizationExtensions
{
    public static IServiceCollection AddPermissionAuthorization(this IServiceCollection services)
        => services.AddSingleton<IAuthorizationHandler, PermissionAuthorizationHandler>();

    public static AuthorizationOptions AddPermissionPolicy(
        this AuthorizationOptions options, string permission, string? legacyPolicy = null)
    {
        options.AddPolicy(permission, policy => policy
            .RequireAuthenticatedUser()
            .AddRequirements(new PermissionRequirement(permission, legacyPolicy)));
        return options;
    }
}