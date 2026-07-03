using Microsoft.Extensions.Logging;
using Watchtower.Application.Abstractions;

namespace Watchtower.Infrastructure.Services;

// Stub — logs to console. Replace with SendGrid/SMTP in production.
public class EmailService : IEmailService
{
    private readonly ILogger<EmailService> _logger;

    public EmailService(ILogger<EmailService> logger) => _logger = logger;

    public Task SendEmailVerificationAsync(string email, string token, CancellationToken ct = default)
    {
        _logger.LogInformation("[EMAIL] Verification token for {Email}: {Token}", email, token);
        return Task.CompletedTask;
    }

    public Task SendPasswordResetAsync(string email, string token, CancellationToken ct = default)
    {
        _logger.LogInformation("[EMAIL] Password reset token for {Email}: {Token}", email, token);
        return Task.CompletedTask;
    }

    public Task SendAlertOpenedAsync(string email, string endpointName, string endpointUrl, string reason, CancellationToken ct = default)
    {
        _logger.LogWarning("[EMAIL] ALERT OPENED → {Email} | [{Name}] {Url} | {Reason}", email, endpointName, endpointUrl, reason);
        return Task.CompletedTask;
    }

    public Task SendAlertResolvedAsync(string email, string endpointName, string endpointUrl, CancellationToken ct = default)
    {
        _logger.LogInformation("[EMAIL] ALERT RESOLVED → {Email} | [{Name}] {Url}", email, endpointName, endpointUrl);
        return Task.CompletedTask;
    }
}
