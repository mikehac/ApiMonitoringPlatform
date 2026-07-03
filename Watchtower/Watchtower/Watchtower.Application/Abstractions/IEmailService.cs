namespace Watchtower.Application.Abstractions;

public interface IEmailService
{
    Task SendEmailVerificationAsync(string email, string token, CancellationToken ct = default);
    Task SendPasswordResetAsync(string email, string token, CancellationToken ct = default);
    Task SendAlertOpenedAsync(string email, string endpointName, string endpointUrl, string reason, CancellationToken ct = default);
    Task SendAlertResolvedAsync(string email, string endpointName, string endpointUrl, CancellationToken ct = default);
}
