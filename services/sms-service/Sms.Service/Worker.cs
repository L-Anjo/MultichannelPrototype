using Alerting.Shared.Configuration;
using Alerting.Shared.Enums;
using Alerting.Shared.Extensions;
using Alerting.Shared.Messaging;
using Alerting.Shared.Models;
using Confluent.Kafka;
using Microsoft.Extensions.Options;

namespace Sms.Service;

public sealed class Worker : BackgroundService
{
    private readonly KafkaConsumerFactory _consumerFactory;
    private readonly IKafkaEventPublisher _publisher;
    private readonly ChannelDispatchSimulator _simulator;
    private readonly DispatcherSimulationOptions _simulationOptions;
    private readonly KafkaOptions _kafkaOptions;
    private readonly ILogger<Worker> _logger;

    public Worker(
        KafkaConsumerFactory consumerFactory,
        IKafkaEventPublisher publisher,
        ChannelDispatchSimulator simulator,
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
        using var consumer = _consumerFactory.Create("sms-service");
        consumer.Subscribe([_kafkaOptions.AlertsProcessedTopic, _kafkaOptions.AlertsDispatchedTopic]);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var result = consumer.Consume(stoppingToken);

                switch (result.Topic)
                {
                    case var topic when topic == _kafkaOptions.AlertsProcessedTopic:
                        await HandleProcessedEventAsync(result.Message.Value, stoppingToken);
                        break;
                    case var topic when topic == _kafkaOptions.AlertsDispatchedTopic:
                        await HandleDispatchResultAsync(result.Message.Value, stoppingToken);
                        break;
                }

                consumer.Commit(result);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (ConsumeException exception)
            {
                _logger.LogError(exception, "Kafka consume error in sms service");
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Unexpected error in sms service");
                await Task.Delay(1000, stoppingToken);
            }
        }
    }

    private async Task HandleProcessedEventAsync(string payload, CancellationToken cancellationToken)
    {
        var processedEvent = JsonMessageSerializer.Deserialize<AlertProcessedEvent>(payload);

        if (processedEvent is null || !IsOwnedChannel(processedEvent.Channel))
        {
            return;
        }

        using (_logger.BeginScope(new Dictionary<string, object>
               {
                   ["eventId"] = processedEvent.EventId,
                   ["deviceId"] = processedEvent.DeviceId,
                   ["userId"] = processedEvent.UserId
               }))
        {
            var dispatchResult = await DispatchWithRetryAsync(
                processedEvent.EventId,
                processedEvent.UserId,
                processedEvent.DeviceId,
                processedEvent.Message,
                processedEvent.Priority,
                processedEvent.Channel,
                processedEvent.AvailableChannels,
                processedEvent.IsFallback,
                processedEvent.PreviousChannel,
                processedEvent.PushToken,
                cancellationToken);

            await PublishResultAsync(dispatchResult, cancellationToken);
        }
    }

    private async Task HandleDispatchResultAsync(string payload, CancellationToken cancellationToken)
    {
        var dispatchResult = JsonMessageSerializer.Deserialize<AlertDispatchResultEvent>(payload);

        if (dispatchResult is null || dispatchResult.Status != DispatchStatus.Failed)
        {
            return;
        }

        var nextChannel = dispatchResult.Channel.NextFallbackChannel();
        if (nextChannel is null)
        {
            await PublishFailedAsync(dispatchResult, cancellationToken);
            return;
        }

        if (!dispatchResult.AvailableChannels.SupportsChannel(nextChannel.Value))
        {
            _logger.LogInformation(
                "Device {DeviceId} does not support fallback channel {Channel}",
                dispatchResult.DeviceId,
                nextChannel.Value);
            await PublishFailedAsync(dispatchResult, cancellationToken);
            return;
        }

        if (!IsOwnedChannel(nextChannel.Value))
        {
            return;
        }

        using (_logger.BeginScope(new Dictionary<string, object>
               {
                   ["eventId"] = dispatchResult.EventId,
                   ["deviceId"] = dispatchResult.DeviceId,
                   ["userId"] = dispatchResult.UserId
               }))
        {
            _logger.LogWarning(
                "Triggering fallback from {FailedChannel} to {NextChannel} for device {DeviceId}",
                dispatchResult.Channel,
                nextChannel.Value,
                dispatchResult.DeviceId);

            var fallbackResult = await DispatchWithRetryAsync(
                dispatchResult.EventId,
                dispatchResult.UserId,
                dispatchResult.DeviceId,
                dispatchResult.Message,
                dispatchResult.Priority,
                nextChannel.Value,
                dispatchResult.AvailableChannels,
                true,
                dispatchResult.Channel,
                dispatchResult.PushToken,
                cancellationToken);

            await PublishResultAsync(fallbackResult, cancellationToken);
        }
    }

    private async Task<AlertDispatchResultEvent> DispatchWithRetryAsync(
        Guid eventId,
        string userId,
        string deviceId,
        string message,
        AlertPriority priority,
        DispatchChannel channel,
        IReadOnlyCollection<DispatchChannel> availableChannels,
        bool isFallback,
        DispatchChannel? previousChannel,
        string? pushToken,
        CancellationToken cancellationToken)
    {
        for (var attempt = 1; attempt <= _simulationOptions.MaxRetries; attempt++)
        {
            if (!_simulator.ShouldFail(channel))
            {
                _logger.LogInformation(
                    "{Channel} dispatch succeeded on attempt {Attempt} for device {DeviceId}",
                    channel,
                    attempt,
                    deviceId);

                return new AlertDispatchResultEvent(
                    EventId: eventId,
                    UserId: userId,
                    DeviceId: deviceId,
                    Message: message,
                    Priority: priority,
                    Channel: channel,
                    Status: DispatchStatus.Succeeded,
                    Attempts: attempt,
                    TimestampUtc: DateTime.UtcNow,
                    AvailableChannels: availableChannels,
                    IsFallback: isFallback,
                    PreviousChannel: previousChannel,
                    PushToken: pushToken);
            }

            _logger.LogWarning(
                "{Channel} dispatch failed on attempt {Attempt} for event {EventId} device {DeviceId}",
                channel,
                attempt,
                eventId,
                deviceId);

            if (attempt < _simulationOptions.MaxRetries)
            {
                var delay = TimeSpan.FromMilliseconds(
                    _simulationOptions.BaseDelayMilliseconds * Math.Pow(2, attempt - 1));
                await Task.Delay(delay, cancellationToken);
            }
        }

        return new AlertDispatchResultEvent(
            EventId: eventId,
            UserId: userId,
            DeviceId: deviceId,
            Message: message,
            Priority: priority,
            Channel: channel,
            Status: DispatchStatus.Failed,
            Attempts: _simulationOptions.MaxRetries,
            TimestampUtc: DateTime.UtcNow,
            AvailableChannels: availableChannels,
            IsFallback: isFallback,
            PreviousChannel: previousChannel,
            PushToken: pushToken,
            FailureReason: $"{channel} dispatch exhausted all retries.");
    }

    private async Task PublishResultAsync(AlertDispatchResultEvent dispatchResult, CancellationToken cancellationToken)
    {
        await _publisher.PublishAsync(
            _kafkaOptions.AlertsDispatchedTopic,
            $"{dispatchResult.EventId}:{dispatchResult.DeviceId}:{dispatchResult.Channel}",
            dispatchResult,
            cancellationToken);

        if (dispatchResult.Status == DispatchStatus.Failed && dispatchResult.Channel.NextFallbackChannel() is null)
        {
            await PublishFailedAsync(dispatchResult, cancellationToken);
        }
    }

    private async Task PublishFailedAsync(AlertDispatchResultEvent dispatchResult, CancellationToken cancellationToken)
    {
        var failedEvent = new AlertFailedEvent(
            EventId: dispatchResult.EventId,
            UserId: dispatchResult.UserId,
            DeviceId: dispatchResult.DeviceId,
            Message: dispatchResult.Message,
            Priority: dispatchResult.Priority,
            FailedChannel: dispatchResult.Channel,
            TimestampUtc: DateTime.UtcNow,
            FailureReason: dispatchResult.FailureReason ?? "Unknown failure.");

        await _publisher.PublishAsync(
            _kafkaOptions.AlertsFailedTopic,
            $"{dispatchResult.EventId}:{dispatchResult.DeviceId}:failed",
            failedEvent,
            cancellationToken);
    }

    private static bool IsOwnedChannel(DispatchChannel channel) =>
        channel is DispatchChannel.Sms or DispatchChannel.Email;
}
