namespace Alerting.Shared.Configuration;

public sealed class DispatcherSimulationOptions
{
    public const string SectionName = "DispatcherSimulation";

    public int MaxRetries { get; set; } = 3;
    public int BaseDelayMilliseconds { get; set; } = 500;
    public double PushFailureRate { get; set; } = 0.3;
    public double SmsFailureRate { get; set; } = 0.2;
    public double EmailFailureRate { get; set; } = 0.1;
}
