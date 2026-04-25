namespace Alerting.Shared.Configuration;

public sealed class KafkaOptions
{
    public const string SectionName = "Kafka";

    public string BootstrapServers { get; set; } = "localhost:9094";
    public string AlertsCreatedTopic { get; set; } = "alerts.created";
    public string AlertsProcessedTopic { get; set; } = "alerts.processed";
    public string AlertsDispatchedTopic { get; set; } = "alerts.dispatched";
    public string AlertsFailedTopic { get; set; } = "alerts.failed";
    public string ClientId { get; set; } = Environment.MachineName;
}
