using System.Security.Claims;
using Auth.Api.Data;

namespace Auth.Api.Services;

public sealed record GeneratedRefreshToken(string Plain, RefreshToken Entity);

public interface IJwtTokenService
{
    string GenerateAccessToken(ApplicationUser user, IEnumerable<string> roles);
    GeneratedRefreshToken GenerateRefreshToken(string userId, string? ipAddress);
    ClaimsPrincipal? ValidateToken(string accessToken);
    string HashToken(string token);
}