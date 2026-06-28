namespace Watchtower.Application.Abstractions;

public interface IEndpointCheckQueue
{
    Task SeedAsync(IEnumerable<Guid> endpointIds, CancellationToken ct = default);
    Task EnqueueAsync(Guid endpointId, int delaySeconds, CancellationToken ct = default);
    Task<IReadOnlyList<Guid>> DequeueDueAsync(int maxCount = 50, CancellationToken ct = default);
    Task<IReadOnlySet<Guid>> GetAllQueuedAsync(CancellationToken ct = default);
    Task RemoveAsync(IEnumerable<Guid> endpointIds, CancellationToken ct = default);
}
