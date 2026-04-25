namespace Alerting.Shared.Messaging;

public interface IKafkaEventPublisher
{
    Task PublishAsync<T>(string topic, string key, T message, CancellationToken cancellationToken = default);
}
