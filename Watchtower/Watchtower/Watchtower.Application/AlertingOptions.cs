namespace Watchtower.Application;

public class AlertingOptions
{
    public const string SectionName = "Alerting";

    public int ConsecutiveFailuresThreshold { get; init; } = 3;
}
