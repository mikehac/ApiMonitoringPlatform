using Watchtower.Application.Events;

namespace Watchtower.Application.Abstractions;

public interface IMonitoringEventPublisher
{
    Task PublishAsync(EndpointStatusChangedEvent evt, CancellationToken ct = default);
    Task PublishAsync(AlertOpenedEvent evt, CancellationToken ct = default);
    Task PublishAsync(AlertResolvedEvent evt, CancellationToken ct = default);
}
