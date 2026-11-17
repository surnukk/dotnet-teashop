namespace Auth.Api.Services;

public interface IEmailService
{
    Task SendEmailConfirmationAsync(string toEmail, string token, CancellationToken ct = default);
    Task SendPasswordResetAsync(string toEmail, string token, CancellationToken ct = default);
}