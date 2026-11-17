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

    public static bool IsAdmin(this ClaimsPrincipal user) => user.IsInRole("Admin");
}