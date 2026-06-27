namespace Watchtower.Domain.Entities;

public class Alert
{
    public Guid Id { get; private set; }
    public Guid EndpointId { get; private set; }
    public AlertState State { get; private set; }
    public string Reason { get; private set; } = default!;
    public DateTime TriggeredAt { get; private set; }
    public DateTime? ResolvedAt { get; private set; }
    public bool EmailSent { get; private set; }

    public MonitoredEndpoint Endpoint { get; private set; } = default!;

    private Alert() { }

    public static Alert Open(Guid endpointId, string reason)
    {
        return new Alert
        {
            Id = Guid.NewGuid(),
            EndpointId = endpointId,
            State = AlertState.Open,
            Reason = reason,
            TriggeredAt = DateTime.UtcNow,
        };
    }

    public void Resolve()
    {
        State = AlertState.Resolved;
        ResolvedAt = DateTime.UtcNow;
    }

    public void MarkEmailSent()
    {
        EmailSent = true;
    }
}
