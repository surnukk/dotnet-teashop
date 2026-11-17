using System.ComponentModel.DataAnnotations;

namespace Auth.Api.Contracts;

public class RegisterRequest
{
    [Required, StringLength(50, MinimumLength = 3)]
    public string UserName { get; set; } = string.Empty;

    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}

public class LoginRequest
{
    [Required] public string UserName { get; set; } = string.Empty;   // username або email
    [Required] public string Password { get; set; } = string.Empty;
}

/// <summary>Необов'язкове тіло: якщо порожнє — токен береться з cookie.</summary>
public class RefreshTokenRequest
{
    public string? RefreshToken { get; set; }
}

public class EmailRequest
{
    [Required, EmailAddress] public string Email { get; set; } = string.Empty;
}

public class VerifyEmailRequest
{
    [Required, EmailAddress] public string Email { get; set; } = string.Empty;
    [Required] public string Token { get; set; } = string.Empty;
}

public class ResetPasswordRequest
{
    [Required, EmailAddress] public string Email { get; set; } = string.Empty;
    [Required] public string Token { get; set; } = string.Empty;
    [Required] public string NewPassword { get; set; } = string.Empty;
}

public class ChangePasswordRequest
{
    [Required] public string CurrentPassword { get; set; } = string.Empty;
    [Required] public string NewPassword { get; set; } = string.Empty;
}

public class UpdateUserRequest
{
    [StringLength(50, MinimumLength = 3)] public string? UserName { get; set; }
    [Phone, StringLength(20)] public string? PhoneNumber { get; set; }
}

public class AssignRoleRequest
{
    [Required] public string Role { get; set; } = string.Empty;
}

public record RegisterResponse(string Id, string UserName, string Email);
public record TokenResponse(string AccessToken, string TokenType, int ExpiresIn);
public record UserDto(string Id, string UserName, string? Email, bool EmailConfirmed,
    string? PhoneNumber, DateTime CreatedAtUtc, IReadOnlyList<string> Roles);
public record PagedResponse<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount);
public record RefreshTokenDto(int Id, DateTime CreatedAtUtc, DateTime ExpiresAtUtc,
    string? CreatedByIp, DateTime? RevokedAtUtc, string? RevokeReason, bool IsActive);

public record AuthResult(string AccessToken, int ExpiresIn, string RefreshToken, DateTime RefreshTokenExpiresUtc);