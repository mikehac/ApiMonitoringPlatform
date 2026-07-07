using System.Text.Json;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using Watchtower.Application.Abstractions;
using Watchtower.Application.Events;

namespace Watchtower.Infrastructure.Services;

public class RedisMonitoringEventPublisher : IMonitoringEventPublisher
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<RedisMonitoringEventPublisher> _logger;

    public RedisMonitoringEventPublisher(
        IConnectionMultiplexer redis,
        ILogger<RedisMonitoringEventPublisher> logger)
    {
        _redis = redis;
        _logger = logger;
    }

    public Task PublishAsync(EndpointStatusChangedEvent evt, CancellationToken ct = default)
        => PublishAsync(MonitoringEventTypes.EndpointStatusChanged, evt);

    public Task PublishAsync(AlertOpenedEvent evt, CancellationToken ct = default)
        => PublishAsync(MonitoringEventTypes.AlertOpened, evt);

    public Task PublishAsync(AlertResolvedEvent evt, CancellationToken ct = default)
        => PublishAsync(MonitoringEventTypes.AlertResolved, evt);

    // Best-effort: a failed publish must never break check processing or alerting.
    private async Task PublishAsync<T>(string type, T payload)
    {
        try
        {
            var envelope = JsonSerializer.Serialize(new { type, payload }, JsonOptions);
            await _redis.GetSubscriber().PublishAsync(
                RedisChannel.Literal(MonitoringEventChannel.Name), envelope);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to publish {EventType} monitoring event.", type);
        }
    }
}
