using System.Security.Claims;

namespace Platform.ServiceDefaults.Security;

public static class UserClaimsExtensions
{
    public static string? GetUserId(this ClaimsPrincipal user)
        => user.FindFirst("sub")?.Value ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value;

    public static string? GetUserName(this ClaimsPrincipal user)
        => user.FindFirst("unique_name")?.Value ?? user.Identity?.Name;

    public static string? GetEmail(this ClaimsPrincipal user)
        => user.FindFirst("email")?.Value ?? user.FindFirst(ClaimTypes.Email)?.Value;

    public static bool IsAdmin(this ClaimsPrincipal user) =>
    user.IsInRole("Admin") || user.HasKeycloakRealmRole("admin");

    // Keycloak кладе ролі у вкладений claim realm_access.roles
    public static bool HasKeycloakRealmRole(this ClaimsPrincipal user, string role)
    {
        var claim = user.FindFirst("realm_access")?.Value;
        if (claim is null) return false;
        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(claim);
            return doc.RootElement.TryGetProperty("roles", out var roles)
                   && roles.EnumerateArray().Any(r => r.GetString() == role);
        }
        catch (System.Text.Json.JsonException) { return false; }
    }
}