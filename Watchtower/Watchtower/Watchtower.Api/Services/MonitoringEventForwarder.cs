using System.Text.Json;
using Microsoft.AspNetCore.SignalR;
using StackExchange.Redis;
using Watchtower.Api.Hubs;
using Watchtower.Application.Events;

namespace Watchtower.Api.Services;

/// <summary>
/// Subscribes to the Redis monitoring-events channel (published by the Worker)
/// and forwards each event to the owning user's SignalR group.
/// </summary>
public class MonitoringEventForwarder : BackgroundService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IConnectionMultiplexer _redis;
    private readonly IHubContext<WatchtowerHub> _hubContext;
    private readonly ILogger<MonitoringEventForwarder> _logger;

    public MonitoringEventForwarder(
        IConnectionMultiplexer redis,
        IHubContext<WatchtowerHub> hubContext,
        ILogger<MonitoringEventForwarder> logger)
    {
        _redis = redis;
        _hubContext = hubContext;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var queue = await _redis.GetSubscriber()
            .SubscribeAsync(RedisChannel.Literal(MonitoringEventChannel.Name));

        queue.OnMessage(HandleMessageAsync);
        _logger.LogInformation("Subscribed to {Channel}.", MonitoringEventChannel.Name);

        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            // Normal shutdown.
        }
        finally
        {
            await queue.UnsubscribeAsync();
        }
    }

    private async Task HandleMessageAsync(ChannelMessage message)
    {
        try
        {
            var envelope = JsonSerializer.Deserialize<Envelope>(message.Message.ToString(), JsonOptions);
            if (envelope is null)
                return;

            switch (envelope.Type)
            {
                case MonitoringEventTypes.EndpointStatusChanged:
                    await ForwardAsync<EndpointStatusChangedEvent>(
                        envelope.Payload, "EndpointStatusChanged", e => e.OwnerId);
                    break;

                case MonitoringEventTypes.AlertOpened:
                    await ForwardAsync<AlertOpenedEvent>(
                        envelope.Payload, "AlertOpened", e => e.OwnerId);
                    break;

                case MonitoringEventTypes.AlertResolved:
                    await ForwardAsync<AlertResolvedEvent>(
                        envelope.Payload, "AlertResolved", e => e.OwnerId);
                    break;

                default:
                    _logger.LogWarning("Unknown monitoring event type {Type}; ignoring.", envelope.Type);
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to forward monitoring event to SignalR clients.");
        }
    }

    private async Task ForwardAsync<T>(JsonElement payload, string clientMethod, Func<T, Guid> ownerId)
    {
        var evt = payload.Deserialize<T>(JsonOptions);
        if (evt is null)
            return;

        await _hubContext.Clients
            .Group(WatchtowerHub.UserGroup(ownerId(evt)))
            .SendAsync(clientMethod, evt);
    }

    private sealed record Envelope(string Type, JsonElement Payload);
}
