using Auth.Api.Contracts;

namespace Auth.Api.Services;

public interface IAuthService
{
    Task<RegisterResponse> RegisterAsync(RegisterRequest request, CancellationToken ct);
    Task<AuthResult> LoginAsync(LoginRequest request, string? ip, CancellationToken ct);
    Task<AuthResult> RefreshAsync(string refreshToken, string? ip, CancellationToken ct);
    Task RevokeTokenAsync(string refreshToken, string requesterId, bool isAdmin, string? ip, CancellationToken ct);
    Task LogoutAsync(string userId, string? refreshToken, string? ip, CancellationToken ct);
    Task VerifyEmailAsync(VerifyEmailRequest request, CancellationToken ct);
    Task ResendVerificationAsync(string email, CancellationToken ct);
    Task ForgotPasswordAsync(string email, CancellationToken ct);
    Task ResetPasswordAsync(ResetPasswordRequest request, string? ip, CancellationToken ct);
    Task ChangePasswordAsync(string userId, ChangePasswordRequest request, string? ip, CancellationToken ct);
}