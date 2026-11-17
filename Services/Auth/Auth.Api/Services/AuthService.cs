using System.Text;
using Auth.Api.Common;
using Auth.Api.Contracts;
using Auth.Api.Data;
using Auth.Api.Options;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Auth.Api.Services;

public class AuthService(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    ApplicationDbContext db,
    IJwtTokenService jwt,
    IEmailService emailService,
    IOptions<JwtSettings> jwtOptions) : IAuthService
{
    private readonly JwtSettings _jwt = jwtOptions.Value;

    public async Task<RegisterResponse> RegisterAsync(RegisterRequest request, CancellationToken ct)
    {
        var user = new ApplicationUser { UserName = request.UserName, Email = request.Email };

        var result = await userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
            throw AuthException.FromIdentity("Registration failed", result);

        await userManager.AddToRoleAsync(user, Roles.User);   // роль User автоматично

        var token = await userManager.GenerateEmailConfirmationTokenAsync(user);
        await emailService.SendEmailConfirmationAsync(user.Email!, EncodeToken(token), ct);

        return new RegisterResponse(user.Id, user.UserName!, user.Email!);
    }

    public async Task<AuthResult> LoginAsync(LoginRequest request, string? ip, CancellationToken ct)
    {
        var user = await userManager.FindByNameAsync(request.UserName)
                   ?? await userManager.FindByEmailAsync(request.UserName);

        if (user is null)
            throw new AuthException(StatusCodes.Status401Unauthorized, "Invalid username or password");

        var check = await signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);

        if (check.IsLockedOut)
            throw new AuthException(StatusCodes.Status423Locked, "Account is temporarily locked. Try again later");
        if (!check.Succeeded)
            throw new AuthException(StatusCodes.Status401Unauthorized, "Invalid username or password");

        if (!user.EmailConfirmed)
            throw new AuthException(StatusCodes.Status403Forbidden, "Email is not confirmed");

        var roles = await userManager.GetRolesAsync(user);
        var refresh = jwt.GenerateRefreshToken(user.Id, ip);
        db.RefreshTokens.Add(refresh.Entity);
        await db.SaveChangesAsync(ct);

        return BuildResult(jwt.GenerateAccessToken(user, roles), refresh);
    }

    public async Task<AuthResult> RefreshAsync(string refreshToken, string? ip, CancellationToken ct)
    {
        var hash = jwt.HashToken(refreshToken);
        var stored = await db.RefreshTokens.Include(t => t.User)
            .SingleOrDefaultAsync(t => t.TokenHash == hash, ct);

        if (stored is null)
            throw new AuthException(StatusCodes.Status401Unauthorized, "Invalid refresh token");

        if (stored.IsRevoked)
        {
            if (stored.ReplacedByTokenHash is not null)
                await RevokeAllForUserAsync(stored.UserId, ip, "Reuse of a rotated refresh token detected", ct);

            throw new AuthException(StatusCodes.Status401Unauthorized, "Invalid refresh token");
        }

        if (stored.IsExpired)
            throw new AuthException(StatusCodes.Status401Unauthorized, "Refresh token expired");

        var user = stored.User;
        if (await userManager.IsLockedOutAsync(user))
            throw new AuthException(StatusCodes.Status423Locked, "Account is temporarily locked");

        var roles = await userManager.GetRolesAsync(user);
        var newRefresh = jwt.GenerateRefreshToken(user.Id, ip);

        stored.RevokedAtUtc = DateTime.UtcNow;
        stored.RevokedByIp = ip;
        stored.ReplacedByTokenHash = newRefresh.Entity.TokenHash;
        stored.RevokeReason = "Replaced by new token";

        db.RefreshTokens.Add(newRefresh.Entity);
        await db.SaveChangesAsync(ct);

        return BuildResult(jwt.GenerateAccessToken(user, roles), newRefresh);
    }

    public async Task RevokeTokenAsync(string refreshToken, string requesterId, bool isAdmin, string? ip, CancellationToken ct)
    {
        var hash = jwt.HashToken(refreshToken);
        var stored = await db.RefreshTokens.SingleOrDefaultAsync(t => t.TokenHash == hash, ct)
                     ?? throw new AuthException(StatusCodes.Status404NotFound, "Refresh token not found");

        if (stored.UserId != requesterId && !isAdmin)
            throw new AuthException(StatusCodes.Status403Forbidden, "You cannot revoke another user's token");

        if (stored.IsRevoked)
            return; 

        stored.RevokedAtUtc = DateTime.UtcNow;
        stored.RevokedByIp = ip;
        stored.RevokeReason = "Revoked by request";
        await db.SaveChangesAsync(ct);
    }

    public async Task LogoutAsync(string userId, string? refreshToken, string? ip, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
            return;

        var hash = jwt.HashToken(refreshToken);
        var stored = await db.RefreshTokens.SingleOrDefaultAsync(t => t.TokenHash == hash && t.UserId == userId, ct);

        if (stored is { IsRevoked: false })
        {
            stored.RevokedAtUtc = DateTime.UtcNow;
            stored.RevokedByIp = ip;
            stored.RevokeReason = "Logout";
            await db.SaveChangesAsync(ct);
        }
    }

    public async Task VerifyEmailAsync(VerifyEmailRequest request, CancellationToken ct)
    {
        var user = await userManager.FindByEmailAsync(request.Email);
        var token = DecodeToken(request.Token);

        if (user is null || token is null)
            throw new AuthException(StatusCodes.Status400BadRequest, "Invalid or expired token");

        if (user.EmailConfirmed)
            return;

        var result = await userManager.ConfirmEmailAsync(user, token);
        if (!result.Succeeded)
            throw new AuthException(StatusCodes.Status400BadRequest, "Invalid or expired token");
    }

    public async Task ResendVerificationAsync(string email, CancellationToken ct)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user is null || user.EmailConfirmed)
            return;

        var token = await userManager.GenerateEmailConfirmationTokenAsync(user);
        await emailService.SendEmailConfirmationAsync(user.Email!, EncodeToken(token), ct);
    }

    public async Task ForgotPasswordAsync(string email, CancellationToken ct)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user is null || !user.EmailConfirmed)
            return;

        var token = await userManager.GeneratePasswordResetTokenAsync(user);
        await emailService.SendPasswordResetAsync(user.Email!, EncodeToken(token), ct);
    }

    public async Task ResetPasswordAsync(ResetPasswordRequest request, string? ip, CancellationToken ct)
    {
        var user = await userManager.FindByEmailAsync(request.Email);
        var token = DecodeToken(request.Token);

        if (user is null || token is null)
            throw new AuthException(StatusCodes.Status400BadRequest, "Invalid or expired token");

        var result = await userManager.ResetPasswordAsync(user, token, request.NewPassword);
        if (!result.Succeeded)
            throw AuthException.FromIdentity("Password reset failed", result);

        await RevokeAllForUserAsync(user.Id, ip, "Password reset", ct);
    }

    public async Task ChangePasswordAsync(string userId, ChangePasswordRequest request, string? ip, CancellationToken ct)
    {
        var user = await userManager.FindByIdAsync(userId)
                   ?? throw new AuthException(StatusCodes.Status401Unauthorized, "User not found");

        var result = await userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
        if (!result.Succeeded)
            throw AuthException.FromIdentity("Password change failed", result);

        await RevokeAllForUserAsync(user.Id, ip, "Password changed", ct);
    }

    private AuthResult BuildResult(string accessToken, GeneratedRefreshToken refresh) =>
        new(accessToken, _jwt.AccessTokenMinutes * 60, refresh.Plain, refresh.Entity.ExpiresAtUtc);

    private async Task RevokeAllForUserAsync(string userId, string? ip, string reason, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var tokens = await db.RefreshTokens
            .Where(t => t.UserId == userId && t.RevokedAtUtc == null && t.ExpiresAtUtc > now)
            .ToListAsync(ct);

        foreach (var t in tokens)
        {
            t.RevokedAtUtc = now;
            t.RevokedByIp = ip;
            t.RevokeReason = reason;
        }
        await db.SaveChangesAsync(ct);
    }

    private static string EncodeToken(string token) =>
        WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));

    private static string? DecodeToken(string token)
    {
        try { return Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(token)); }
        catch (FormatException) { return null; }
    }
}