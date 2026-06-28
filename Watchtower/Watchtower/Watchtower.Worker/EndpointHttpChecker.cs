using Watchtower.Domain.Entities;
using Watchtower.Domain.Enums;

namespace Watchtower.Worker;

public class EndpointHttpChecker
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<EndpointHttpChecker> _logger;

    public EndpointHttpChecker(IHttpClientFactory httpClientFactory, ILogger<EndpointHttpChecker> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<(CheckResult Result, EndpointStatus NewStatus)> CheckAsync(
        MonitoredEndpoint endpoint, CancellationToken ct)
    {
        var httpClient = _httpClientFactory.CreateClient("watcher");
        var request = new HttpRequestMessage(new HttpMethod(endpoint.HttpMethod), endpoint.Url);

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(endpoint.TimeoutSeconds));

        var sw = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseContentRead, timeoutCts.Token);
            sw.Stop();

            var statusCode = (int)response.StatusCode;
            var responseTimeMs = sw.ElapsedMilliseconds;

            if (endpoint.ExpectedStatusCode.HasValue && statusCode != endpoint.ExpectedStatusCode.Value)
            {
                return (
                    CheckResult.Failure(endpoint.Id, responseTimeMs,
                        $"Expected status {endpoint.ExpectedStatusCode}, got {statusCode}", statusCode),
                    EndpointStatus.Down);
            }

            if (!string.IsNullOrEmpty(endpoint.ExpectedBodyContains))
            {
                var body = await response.Content.ReadAsStringAsync(ct);
                if (!body.Contains(endpoint.ExpectedBodyContains, StringComparison.OrdinalIgnoreCase))
                {
                    return (
                        CheckResult.Failure(endpoint.Id, responseTimeMs,
                            $"Response body does not contain: '{endpoint.ExpectedBodyContains}'", statusCode),
                        EndpointStatus.Down);
                }
            }

            if (endpoint.MaxResponseTimeMs.HasValue && responseTimeMs > endpoint.MaxResponseTimeMs.Value)
                return (CheckResult.Success(endpoint.Id, statusCode, responseTimeMs), EndpointStatus.Degraded);

            return (CheckResult.Success(endpoint.Id, statusCode, responseTimeMs), EndpointStatus.Up);
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            sw.Stop();
            return (
                CheckResult.Failure(endpoint.Id, sw.ElapsedMilliseconds,
                    $"Request timed out after {endpoint.TimeoutSeconds}s"),
                EndpointStatus.Down);
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogWarning("Check failed for {EndpointId} ({Url}): {Error}", endpoint.Id, endpoint.Url, ex.Message);
            return (CheckResult.Failure(endpoint.Id, sw.ElapsedMilliseconds, ex.Message), EndpointStatus.Down);
        }
    }
}
