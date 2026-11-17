namespace Auth.Api.Services;

public class LoggingEmailService(ILogger<LoggingEmailService> logger, IConfiguration config) : IEmailService
{
    private string FrontendUrl => config["App:FrontendUrl"] ?? "http://localhost:3000";

    public Task SendEmailConfirmationAsync(string toEmail, string token, CancellationToken ct = default)
    {
        var link = $"{FrontendUrl}/verify-email?email={Uri.EscapeDataString(toEmail)}&token={token}";
        logger.LogInformation("[MOCK EMAIL] To: {To} | Subject: Confirm your email | Link: {Link} | Token: {Token}",
            toEmail, link, token);
        return Task.CompletedTask;
    }

    public Task SendPasswordResetAsync(string toEmail, string token, CancellationToken ct = default)
    {
        var link = $"{FrontendUrl}/reset-password?email={Uri.EscapeDataString(toEmail)}&token={token}";
        logger.LogInformation("[MOCK EMAIL] To: {To} | Subject: Reset your password | Link: {Link} | Token: {Token}",
            toEmail, link, token);
        return Task.CompletedTask;
    }
}