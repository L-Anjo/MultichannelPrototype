using Alerting.Shared.Configuration;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Alerting.Shared.Messaging;

public sealed class KafkaEventPublisher : IKafkaEventPublisher, IDisposable
{
    private readonly IProducer<string, string> _producer;
    private readonly ILogger<KafkaEventPublisher> _logger;

    public KafkaEventPublisher(IOptions<KafkaOptions> options, ILogger<KafkaEventPublisher> logger)
    {
        _logger = logger;
        var producerConfig = new ProducerConfig
        {
            BootstrapServers = options.Value.BootstrapServers,
            ClientId = options.Value.ClientId,
            Acks = Acks.All,
            EnableIdempotence = false
        };

        _producer = new ProducerBuilder<string, string>(producerConfig).Build();
    }

    public async Task PublishAsync<T>(string topic, string key, T message, CancellationToken cancellationToken = default)
    {
        var payload = JsonMessageSerializer.Serialize(message);
        var result = await _producer.ProduceAsync(
            topic,
            new Message<string, string> { Key = key, Value = payload },
            cancellationToken);

        _logger.LogInformation(
            "Published message to Kafka topic {Topic} partition {Partition} offset {Offset} key {Key}",
            topic,
            result.Partition.Value,
            result.Offset.Value,
            key);
    }

    public void Dispose()
    {
        _producer.Flush(TimeSpan.FromSeconds(5));
        _producer.Dispose();
    }
}
