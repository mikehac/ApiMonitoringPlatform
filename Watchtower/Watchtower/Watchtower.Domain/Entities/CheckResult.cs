namespace Watchtower.Domain.Entities;

public class CheckResult
{
    public Guid Id { get; private set; }
    public Guid EndpointId { get; private set; }
    public DateTime CheckedAt { get; private set; }
    public bool IsSuccess { get; private set; }
    public int? StatusCode { get; private set; }
    public long ResponseTimeMs { get; private set; }
    public string? ErrorMessage { get; private set; }

    public MonitoredEndpoint Endpoint { get; private set; } = default!;

    private CheckResult() { }

    public static CheckResult Success(Guid endpointId, int statusCode, long responseTimeMs)
    {
        return new CheckResult
        {
            Id = Guid.NewGuid(),
            EndpointId = endpointId,
            CheckedAt = DateTime.UtcNow,
            IsSuccess = true,
            StatusCode = statusCode,
            ResponseTimeMs = responseTimeMs,
        };
    }

    public static CheckResult Failure(Guid endpointId, long responseTimeMs, string errorMessage, int? statusCode = null)
    {
        return new CheckResult
        {
            Id = Guid.NewGuid(),
            EndpointId = endpointId,
            CheckedAt = DateTime.UtcNow,
            IsSuccess = false,
            StatusCode = statusCode,
            ResponseTimeMs = responseTimeMs,
            ErrorMessage = errorMessage,
        };
    }
}
