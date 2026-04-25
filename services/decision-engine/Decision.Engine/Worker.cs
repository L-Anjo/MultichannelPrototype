using Alerting.Shared.Configuration;
using Alerting.Shared.Extensions;
using Alerting.Shared.Messaging;
using Alerting.Shared.Models;
using Confluent.Kafka;
using Microsoft.Extensions.Options;

namespace Decision.Engine;

public sealed class Worker : BackgroundService
{
    private readonly KafkaConsumerFactory _consumerFactory;
    private readonly IKafkaEventPublisher _publisher;
    private readonly KafkaOptions _kafkaOptions;
    private readonly ILogger<Worker> _logger;

    public Worker(
        KafkaConsumerFactory consumerFactory,
        IKafkaEventPublisher publisher,
        IOptions<KafkaOptions> kafkaOptions,
        ILogger<Worker> logger)
    {
        _consumerFactory = consumerFactory;
        _publisher = publisher;
        _kafkaOptions = kafkaOptions.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var consumer = _consumerFactory.Create("decision-engine");
        consumer.Subscribe(_kafkaOptions.AlertsCreatedTopic);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var result = consumer.Consume(stoppingToken);
                var createdEvent = JsonMessageSerializer.Deserialize<AlertCreatedEvent>(result.Message.Value);

                if (createdEvent is null)
                {
                    _logger.LogWarning("Skipping unreadable event from topic {Topic}", result.Topic);
                    consumer.Commit(result);
                    continue;
                }

                var processedEvent = new AlertProcessedEvent(
                    EventId: createdEvent.EventId,
                    Message: createdEvent.Message,
                    Priority: createdEvent.Priority,
                    TimestampUtc: DateTime.UtcNow,
                    RequestedChannels: createdEvent.Priority.ToChannels(),
                    Source: createdEvent.Source,
                    OriginalFormat: createdEvent.OriginalFormat);

                using (_logger.BeginScope(new Dictionary<string, object> { ["eventId"] = createdEvent.EventId }))
                {
                    _logger.LogInformation(
                        "Decision engine mapped priority {Priority} to channels {Channels}",
                        createdEvent.Priority,
                        string.Join(",", processedEvent.RequestedChannels));

                    await _publisher.PublishAsync(
                        _kafkaOptions.AlertsProcessedTopic,
                        createdEvent.EventId.ToString(),
                        processedEvent,
                        stoppingToken);
                }

                consumer.Commit(result);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (ConsumeException exception)
            {
                _logger.LogError(exception, "Kafka consume error in decision engine");
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Unexpected error in decision engine");
                await Task.Delay(1000, stoppingToken);
            }
        }
    }
}
