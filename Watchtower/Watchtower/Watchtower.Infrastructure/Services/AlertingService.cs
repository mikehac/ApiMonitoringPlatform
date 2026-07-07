using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Watchtower.Application;
using Watchtower.Application.Abstractions;
using Watchtower.Application.Events;
using Watchtower.Domain.Entities;
using Watchtower.Domain.Enums;

namespace Watchtower.Infrastructure.Services;

public class AlertingService : IAlertingService
{
    private readonly IApplicationDbContext _db;
    private readonly IEmailService _emailService;
    private readonly IMonitoringEventPublisher _eventPublisher;
    private readonly AlertingOptions _options;
    private readonly ILogger<AlertingService> _logger;

    public AlertingService(
        IApplicationDbContext db,
        IEmailService emailService,
        IMonitoringEventPublisher eventPublisher,
        IOptions<AlertingOptions> options,
        ILogger<AlertingService> logger)
    {
        _db = db;
        _emailService = emailService;
        _eventPublisher = eventPublisher;
        _options = options.Value;
        _logger = logger;
    }

    public async Task HandleAsync(MonitoredEndpoint endpoint, EndpointStatus previousStatus, CancellationToken ct = default)
    {
        var currentStatus = endpoint.Status;

        if (currentStatus == EndpointStatus.Down
            && endpoint.ConsecutiveFailures >= _options.ConsecutiveFailuresThreshold)
        {
            await TryOpenAlertAsync(endpoint, ct);
        }
        else if (currentStatus != EndpointStatus.Down && previousStatus == EndpointStatus.Down)
        {
            await TryResolveAlertAsync(endpoint, ct);
        }
    }

    private async Task TryOpenAlertAsync(MonitoredEndpoint endpoint, CancellationToken ct)
    {
        var alreadyOpen = await _db.Alerts
            .AnyAsync(a => a.EndpointId == endpoint.Id && a.State == AlertState.Open, ct);

        if (alreadyOpen)
            return;

        var reason = $"Endpoint down after {endpoint.ConsecutiveFailures} consecutive failures.";
        var alert = Alert.Open(endpoint.Id, reason);
        _db.Alerts.Add(alert);
        await _db.SaveChangesAsync(ct);

        _logger.LogWarning("Alert opened for [{Name}] {Url}: {Reason}", endpoint.Name, endpoint.Url, reason);

        await _eventPublisher.PublishAsync(new AlertOpenedEvent(
            endpoint.OwnerId,
            alert.Id,
            endpoint.Id,
            endpoint.Name,
            endpoint.Url,
            alert.Reason,
            alert.TriggeredAt), ct);

        var ownerEmail = await GetOwnerEmailAsync(endpoint.OwnerId, ct);
        if (ownerEmail is not null)
            await TrySendEmailAsync(
                () => _emailService.SendAlertOpenedAsync(ownerEmail, endpoint.Name, endpoint.Url, reason, ct),
                alert, ct);
    }

    private async Task TryResolveAlertAsync(MonitoredEndpoint endpoint, CancellationToken ct)
    {
        var alert = await _db.Alerts
            .FirstOrDefaultAsync(a => a.EndpointId == endpoint.Id && a.State == AlertState.Open, ct);

        if (alert is null)
            return;

        alert.Resolve();
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Alert resolved for [{Name}] {Url}.", endpoint.Name, endpoint.Url);

        await _eventPublisher.PublishAsync(new AlertResolvedEvent(
            endpoint.OwnerId,
            alert.Id,
            endpoint.Id,
            endpoint.Name,
            endpoint.Url,
            alert.TriggeredAt,
            alert.ResolvedAt!.Value,
            (alert.ResolvedAt.Value - alert.TriggeredAt).TotalSeconds), ct);

        var ownerEmail = await GetOwnerEmailAsync(endpoint.OwnerId, ct);
        if (ownerEmail is not null)
            await TrySendEmailAsync(
                () => _emailService.SendAlertResolvedAsync(ownerEmail, endpoint.Name, endpoint.Url, ct),
                alert, ct);
    }

    private async Task TrySendEmailAsync(Func<Task> send, Alert alert, CancellationToken ct)
    {
        try
        {
            await send();
            alert.MarkEmailSent();
            await _db.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send alert email for alert {AlertId}.", alert.Id);
        }
    }

    private async Task<string?> GetOwnerEmailAsync(Guid ownerId, CancellationToken ct)
        => await _db.Users
            .Where(u => u.Id == ownerId)
            .Select(u => u.Email)
            .FirstOrDefaultAsync(ct);
}
