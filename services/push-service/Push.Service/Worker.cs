using Alerting.Shared.Configuration;
using Alerting.Shared.Enums;
using Alerting.Shared.Messaging;
using Alerting.Shared.Models;
using Confluent.Kafka;
using Microsoft.Extensions.Options;

namespace Push.Service;

public sealed class Worker : BackgroundService
{
    private readonly KafkaConsumerFactory _consumerFactory;
    private readonly IKafkaEventPublisher _publisher;
    private readonly PushDispatchSimulator _simulator;
    private readonly DispatcherSimulationOptions _simulationOptions;
    private readonly KafkaOptions _kafkaOptions;
    private readonly ILogger<Worker> _logger;

    public Worker(
        KafkaConsumerFactory consumerFactory,
        IKafkaEventPublisher publisher,
        PushDispatchSimulator simulator,
        IOptions<DispatcherSimulationOptions> simulationOptions,
        IOptions<KafkaOptions> kafkaOptions,
        ILogger<Worker> logger)
    {
        _consumerFactory = consumerFactory;
        _publisher = publisher;
        _simulator = simulator;
        _simulationOptions = simulationOptions.Value;
        _kafkaOptions = kafkaOptions.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var consumer = _consumerFactory.Create("push-service");
        consumer.Subscribe(_kafkaOptions.AlertsProcessedTopic);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var result = consumer.Consume(stoppingToken);
                var processedEvent = JsonMessageSerializer.Deserialize<AlertProcessedEvent>(result.Message.Value);

                if (processedEvent is null || !processedEvent.RequestedChannels.Contains(DispatchChannel.Push))
                {
                    consumer.Commit(result);
                    continue;
                }

                using (_logger.BeginScope(new Dictionary<string, object> { ["eventId"] = processedEvent.EventId }))
                {
                    var dispatchResult = await DispatchWithRetryAsync(processedEvent, stoppingToken);

                    await _publisher.PublishAsync(
                        _kafkaOptions.AlertsDispatchedTopic,
                        processedEvent.EventId.ToString(),
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
                _logger.LogError(exception, "Kafka consume error in push service");
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Unexpected error in push service");
                await Task.Delay(1000, stoppingToken);
            }
        }
    }

    private async Task<AlertDispatchResultEvent> DispatchWithRetryAsync(
        AlertProcessedEvent processedEvent,
        CancellationToken cancellationToken)
    {
        for (var attempt = 1; attempt <= _simulationOptions.MaxRetries; attempt++)
        {
            if (!_simulator.ShouldFail())
            {
                _logger.LogInformation(
                    "Push dispatch succeeded on attempt {Attempt} for message {Message}",
                    attempt,
                    processedEvent.Message);

                return new AlertDispatchResultEvent(
                    EventId: processedEvent.EventId,
                    Message: processedEvent.Message,
                    Priority: processedEvent.Priority,
                    Channel: DispatchChannel.Push,
                    Status: DispatchStatus.Succeeded,
                    Attempts: attempt,
                    TimestampUtc: DateTime.UtcNow,
                    RequestedChannels: processedEvent.RequestedChannels);
            }

            _logger.LogWarning(
                "Push dispatch failed on attempt {Attempt} for event {EventId}",
                attempt,
                processedEvent.EventId);

            if (attempt < _simulationOptions.MaxRetries)
            {
                var delay = TimeSpan.FromMilliseconds(
                    _simulationOptions.BaseDelayMilliseconds * Math.Pow(2, attempt - 1));
                await Task.Delay(delay, cancellationToken);
            }
        }

        return new AlertDispatchResultEvent(
            EventId: processedEvent.EventId,
            Message: processedEvent.Message,
            Priority: processedEvent.Priority,
            Channel: DispatchChannel.Push,
            Status: DispatchStatus.Failed,
            Attempts: _simulationOptions.MaxRetries,
            TimestampUtc: DateTime.UtcNow,
            RequestedChannels: processedEvent.RequestedChannels,
            FailureReason: "Push dispatch exhausted all retries.");
    }
}
