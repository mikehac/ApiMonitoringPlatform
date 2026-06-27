namespace Watchtower.Worker;

// Phase 3: will poll due endpoints from Redis queue and dispatch health checks.
// Stub keeps the project buildable while Phase 2 (auth + CRUD) is implemented.
public class CheckSchedulerService : BackgroundService
{
    private readonly ILogger<CheckSchedulerService> _logger;

    public CheckSchedulerService(ILogger<CheckSchedulerService> logger)
    {
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("CheckSchedulerService started — awaiting implementation in Phase 3.");
        await Task.Delay(Timeout.Infinite, stoppingToken);
    }
}
