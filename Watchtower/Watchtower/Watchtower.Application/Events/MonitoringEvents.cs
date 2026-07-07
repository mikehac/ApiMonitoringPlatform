namespace Watchtower.Application.Events;

/// <summary>
/// Redis pub/sub channel carrying monitoring events from the Worker to the Api,
/// which forwards them to SignalR clients.
/// </summary>
public static class MonitoringEventChannel
{
    public const string Name = "watchtower:monitoring-events";
}

public static class MonitoringEventTypes
{
    public const string EndpointStatusChanged = "endpoint-status-changed";
    public const string AlertOpened = "alert-opened";
    public const string AlertResolved = "alert-resolved";
}

public sealed record EndpointStatusChangedEvent(
    Guid OwnerId,
    Guid EndpointId,
    string EndpointName,
    string Url,
    string PreviousStatus,
    string NewStatus,
    long ResponseTimeMs,
    DateTime CheckedAt);

public sealed record AlertOpenedEvent(
    Guid OwnerId,
    Guid AlertId,
    Guid EndpointId,
    string EndpointName,
    string Url,
    string Reason,
    DateTime TriggeredAt);

public sealed record AlertResolvedEvent(
    Guid OwnerId,
    Guid AlertId,
    Guid EndpointId,
    string EndpointName,
    string Url,
    DateTime TriggeredAt,
    DateTime ResolvedAt,
    double DurationSeconds);
