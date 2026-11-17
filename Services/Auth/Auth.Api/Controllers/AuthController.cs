using System.Text.Json;
using Auth.Api.Common;
using Auth.Api.Contracts;
using Auth.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Platform.ServiceDefaults.Security;

namespace Auth.Api.Controllers;

[ApiController]
[Route("auth")]
public class AuthController(IAuthService auth, IWebHostEnvironment env) : ControllerBase
{
    private const string RefreshCookie = "refreshToken";

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<ActionResult<RegisterResponse>> Register(RegisterRequest request, CancellationToken ct)
    {
        var result = await auth.RegisterAsync(request, ct);
        return Created($"/users/{result.Id}", result);
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<TokenResponse>> Login(LoginRequest request, CancellationToken ct)
    {
        var result = await auth.LoginAsync(request, ClientIp, ct);
        return RespondWithTokens(result);
    }

    [HttpPost("refresh-token")]
    [AllowAnonymous]
    public async Task<ActionResult<TokenResponse>> Refresh(CancellationToken ct)
    {
        var token = await ReadRefreshTokenAsync(ct);
        if (string.IsNullOrWhiteSpace(token))
            throw new AuthException(StatusCodes.Status401Unauthorized, "Refresh token is missing");

        var result = await auth.RefreshAsync(token, ClientIp, ct);
        return RespondWithTokens(result);
    }

    [HttpPost("revoke-token")]
    [Authorize]
    public async Task<IActionResult> RevokeToken(CancellationToken ct)
    {
        var token = await ReadRefreshTokenAsync(ct);
        if (string.IsNullOrWhiteSpace(token))
            throw new AuthException(StatusCodes.Status400BadRequest, "Refresh token is required (cookie or body)");

        await auth.RevokeTokenAsync(token, User.GetUserId()!, User.IsAdmin(), ClientIp, ct);
        return NoContent();
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout(CancellationToken ct)
    {
        var token = await ReadRefreshTokenAsync(ct);
        await auth.LogoutAsync(User.GetUserId()!, token, ClientIp, ct);

        Response.Cookies.Delete(RefreshCookie, new CookieOptions { Path = "/auth" });
        return NoContent();
    }

    [HttpPost("verify-email")]
    [AllowAnonymous]
    public async Task<IActionResult> VerifyEmail(VerifyEmailRequest request, CancellationToken ct)
    {
        await auth.VerifyEmailAsync(request, ct);
        return NoContent();
    }

    [HttpPost("resend-verification")]
    [AllowAnonymous]
    public async Task<IActionResult> ResendVerification(EmailRequest request, CancellationToken ct)
    {
        await auth.ResendVerificationAsync(request.Email, ct);
        return Ok(new { message = "If the account exists and is not confirmed, an email has been sent." });
    }

    [HttpPost("forgot-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ForgotPassword(EmailRequest request, CancellationToken ct)
    {
        await auth.ForgotPasswordAsync(request.Email, ct);
        return Ok(new { message = "If the account exists, a password reset email has been sent." });
    }

    [HttpPost("reset-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ResetPassword(ResetPasswordRequest request, CancellationToken ct)
    {
        await auth.ResetPasswordAsync(request, ClientIp, ct);
        return NoContent();
    }

    [HttpPost("change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword(ChangePasswordRequest request, CancellationToken ct)
    {
        await auth.ChangePasswordAsync(User.GetUserId()!, request, ClientIp, ct);
        return NoContent();
    }

    private string? ClientIp =>
        Request.Headers["X-Forwarded-For"].FirstOrDefault()?.Split(',')[0].Trim()  
        ?? HttpContext.Connection.RemoteIpAddress?.ToString();

    private ActionResult<TokenResponse> RespondWithTokens(AuthResult result)
    {
        Response.Cookies.Append(RefreshCookie, result.RefreshToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = !env.IsDevelopment(),    
            SameSite = SameSiteMode.Strict,
            Path = "/auth",                  
            Expires = new DateTimeOffset(DateTime.SpecifyKind(result.RefreshTokenExpiresUtc, DateTimeKind.Utc))
        });

        return Ok(new TokenResponse(result.AccessToken, "Bearer", result.ExpiresIn));
    }

    private async Task<string?> ReadRefreshTokenAsync(CancellationToken ct)
    {
        if (Request.HasJsonContentType())
        {
            try
            {
                var body = await Request.ReadFromJsonAsync<RefreshTokenRequest>(ct);
                if (!string.IsNullOrWhiteSpace(body?.RefreshToken))
                    return body.RefreshToken;
            }
            catch (JsonException) { /* порожнє або некоректне тіло — ігноруємо */ }
        }
        return Request.Cookies[RefreshCookie];
    }
}