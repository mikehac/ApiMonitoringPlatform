using Microsoft.EntityFrameworkCore;
using Watchtower.Application.Abstractions;
using Watchtower.Domain.Entities;
using Watchtower.Domain.Enums;

namespace Watchtower.Worker;

public class CheckSchedulerService : BackgroundService
{
    private const int PollingIntervalMs = 1000;
    private const int SyncIntervalSeconds = 30;
    private const int ErrorRequeueDelaySeconds = 60;
    private const int MaxConcurrentChecks = 50;

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IEndpointCheckQueue _checkQueue;
    private readonly EndpointHttpChecker _checker;
    private readonly ILogger<CheckSchedulerService> _logger;

    public CheckSchedulerService(
        IServiceScopeFactory scopeFactory,
        IEndpointCheckQueue checkQueue,
        EndpointHttpChecker checker,
        ILogger<CheckSchedulerService> logger)
    {
        _scopeFactory = scopeFactory;
        _checkQueue = checkQueue;
        _checker = checker;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("CheckSchedulerService starting.");

        await SeedQueueAsync(stoppingToken);

        var lastSync = DateTime.MinValue;
        using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(PollingIntervalMs));

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            if ((DateTime.UtcNow - lastSync).TotalSeconds >= SyncIntervalSeconds)
            {
                await SyncQueueWithDatabaseAsync(stoppingToken);
                lastSync = DateTime.UtcNow;
            }

            var dueIds = await _checkQueue.DequeueDueAsync(MaxConcurrentChecks, stoppingToken);
            if (dueIds.Count == 0)
                continue;

            await Task.WhenAll(dueIds.Select(id => ProcessEndpointAsync(id, stoppingToken)));
        }

        _logger.LogInformation("CheckSchedulerService stopped.");
    }

    private async Task SeedQueueAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();

        var ids = await db.Endpoints
            .Where(e => e.IsActive)
            .Select(e => e.Id)
            .ToListAsync(ct);

        await _checkQueue.SeedAsync(ids, ct);
        _logger.LogInformation("Seeded check queue with {Count} active endpoints.", ids.Count);
    }

    private async Task SyncQueueWithDatabaseAsync(CancellationToken ct)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();

            var activeIds = (await db.Endpoints
                .Where(e => e.IsActive)
                .Select(e => e.Id)
                .ToListAsync(ct))
                .ToHashSet();

            var queuedIds = await _checkQueue.GetAllQueuedAsync(ct);

            var toAdd = activeIds.Except(queuedIds).ToList();
            foreach (var id in toAdd)
            {
                await _checkQueue.EnqueueAsync(id, 0, ct);
                _logger.LogInformation("Queued new active endpoint {EndpointId}.", id);
            }

            var toRemove = queuedIds.Except(activeIds).ToList();
            if (toRemove.Count > 0)
            {
                await _checkQueue.RemoveAsync(toRemove, ct);
                _logger.LogInformation("Removed {Count} inactive endpoint(s) from queue.", toRemove.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing check queue with database.");
        }
    }

    private async Task ProcessEndpointAsync(Guid endpointId, CancellationToken ct)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
            var alertingService = scope.ServiceProvider.GetRequiredService<IAlertingService>();

            var endpoint = await db.Endpoints
                .FirstOrDefaultAsync(e => e.Id == endpointId, ct);

            if (endpoint is null || !endpoint.IsActive)
            {
                _logger.LogDebug("Endpoint {EndpointId} not found or inactive; skipping.", endpointId);
                return;
            }

            var previousStatus = endpoint.Status;
            var (checkResult, newStatus) = await _checker.CheckAsync(endpoint, ct);

            db.CheckResults.Add(checkResult);
            endpoint.RecordCheckResult(newStatus);
            await db.SaveChangesAsync(ct);

            _logger.LogInformation(
                "Checked [{Name}] {Url} — {Status} in {Ms}ms",
                endpoint.Name, endpoint.Url, newStatus, checkResult.ResponseTimeMs);

            await alertingService.HandleAsync(endpoint, previousStatus, ct);

            await _checkQueue.EnqueueAsync(endpointId, endpoint.CheckIntervalSeconds, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled error processing endpoint {EndpointId}; requeuing in {Delay}s.",
                endpointId, ErrorRequeueDelaySeconds);
            await _checkQueue.EnqueueAsync(endpointId, ErrorRequeueDelaySeconds, ct);
        }
    }
}
