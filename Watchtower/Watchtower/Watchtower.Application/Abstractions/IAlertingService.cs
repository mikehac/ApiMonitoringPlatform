using Watchtower.Domain.Entities;
using Watchtower.Domain.Enums;

namespace Watchtower.Application.Abstractions;

public interface IAlertingService
{
    Task HandleAsync(MonitoredEndpoint endpoint, EndpointStatus previousStatus, CancellationToken ct = default);
}
