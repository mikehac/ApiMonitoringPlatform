namespace Watchtower.Domain.Entities;

public class MonitoredEndpoint
{
    public Guid Id { get; private set; }
    public Guid OwnerId { get; private set; }
    public string Name { get; private set; } = default!;
    public string Url { get; private set; } = default!;
    public string HttpMethod { get; private set; } = default!;
    public int CheckIntervalSeconds { get; private set; }
    public int TimeoutSeconds { get; private set; }
    public int? ExpectedStatusCode { get; private set; }
    public string? ExpectedBodyContains { get; private set; }
    public int? MaxResponseTimeMs { get; private set; }
    public EndpointStatus Status { get; private set; }
    public DateTime? LastCheckedAt { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    public User Owner { get; private set; } = default!;
    public ICollection<CheckResult> CheckResults { get; private set; } = [];
    public ICollection<Alert> Alerts { get; private set; } = [];

    private MonitoredEndpoint() { }

    public static MonitoredEndpoint Create(
        Guid ownerId,
        string name,
        string url,
        string httpMethod,
        int checkIntervalSeconds,
        int timeoutSeconds = 30,
        int? expectedStatusCode = 200,
        string? expectedBodyContains = null,
        int? maxResponseTimeMs = null)
    {
        return new MonitoredEndpoint
        {
            Id = Guid.NewGuid(),
            OwnerId = ownerId,
            Name = name,
            Url = url,
            HttpMethod = httpMethod.ToUpperInvariant(),
            CheckIntervalSeconds = checkIntervalSeconds,
            TimeoutSeconds = timeoutSeconds,
            ExpectedStatusCode = expectedStatusCode,
            ExpectedBodyContains = expectedBodyContains,
            MaxResponseTimeMs = maxResponseTimeMs,
            Status = EndpointStatus.Unknown,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };
    }

    public void UpdateSettings(
        string name,
        string url,
        string httpMethod,
        int checkIntervalSeconds,
        int timeoutSeconds,
        int? expectedStatusCode,
        string? expectedBodyContains,
        int? maxResponseTimeMs)
    {
        Name = name;
        Url = url;
        HttpMethod = httpMethod.ToUpperInvariant();
        CheckIntervalSeconds = checkIntervalSeconds;
        TimeoutSeconds = timeoutSeconds;
        ExpectedStatusCode = expectedStatusCode;
        ExpectedBodyContains = expectedBodyContains;
        MaxResponseTimeMs = maxResponseTimeMs;
        UpdatedAt = DateTime.UtcNow;
    }

    public void RecordCheckResult(EndpointStatus newStatus)
    {
        Status = newStatus;
        LastCheckedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Activate()
    {
        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
    }
}
