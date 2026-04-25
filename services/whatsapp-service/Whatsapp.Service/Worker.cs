using Alerting.Shared.Configuration;
using Alerting.Shared.Enums;
using Alerting.Shared.Messaging;
using Alerting.Shared.Models;
using Confluent.Kafka;
using Microsoft.Extensions.Options;

namespace Whatsapp.Service;

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
        using var consumer = _consumerFactory.Create("whatsapp-service");
        consumer.Subscribe(_kafkaOptions.AlertsProcessedTopic);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var result = consumer.Consume(stoppingToken);
                var processedEvent = JsonMessageSerializer.Deserialize<AlertProcessedEvent>(result.Message.Value);

                if (processedEvent is null || processedEvent.Channel != DispatchChannel.Whatsapp)
                {
                    consumer.Commit(result);
                    continue;
                }

                using (_logger.BeginScope(new Dictionary<string, object> { ["eventId"] = processedEvent.EventId, ["deviceId"] = processedEvent.DeviceId }))
                {
                    _logger.LogInformation(
                        "WhatsApp mock dispatch for user {UserId} device {DeviceId} message {Message}",
                        processedEvent.UserId,
                        processedEvent.DeviceId,
                        processedEvent.Message);

                    var dispatchResult = new AlertDispatchResultEvent(
                        EventId: processedEvent.EventId,
                        UserId: processedEvent.UserId,
                        DeviceId: processedEvent.DeviceId,
                        Message: processedEvent.Message,
                        Priority: processedEvent.Priority,
                        Channel: DispatchChannel.Whatsapp,
                        Status: DispatchStatus.Succeeded,
                        Attempts: 1,
                        TimestampUtc: DateTime.UtcNow,
                        AvailableChannels: processedEvent.AvailableChannels,
                        IsFallback: processedEvent.IsFallback,
                        PreviousChannel: processedEvent.PreviousChannel,
                        PushToken: processedEvent.PushToken);

                    await _publisher.PublishAsync(
                        _kafkaOptions.AlertsDispatchedTopic,
                        $"{processedEvent.EventId}:{processedEvent.DeviceId}:whatsapp",
                        dispatchResult,
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
                _logger.LogError(exception, "Kafka consume error in whatsapp service");
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Unexpected error in whatsapp service");
                await Task.Delay(1000, stoppingToken);
            }
        }
    }
}
