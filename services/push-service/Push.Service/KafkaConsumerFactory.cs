using Alerting.Shared.Configuration;
using Confluent.Kafka;
using Microsoft.Extensions.Options;

namespace Push.Service;

public sealed class KafkaConsumerFactory
{
    private readonly KafkaOptions _options;

    public KafkaConsumerFactory(IOptions<KafkaOptions> options)
    {
        _options = options.Value;
    }

    public IConsumer<string, string> Create(string groupId)
    {
        var config = new ConsumerConfig
        {
            BootstrapServers = _options.BootstrapServers,
            GroupId = groupId,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false,
            AllowAutoCreateTopics = false
        };

        return new ConsumerBuilder<string, string>(config).Build();
    }
}
