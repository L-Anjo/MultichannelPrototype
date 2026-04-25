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
    private readonly DeviceProjectionStore _deviceProjectionStore;
    private readonly KafkaOptions _kafkaOptions;
    private readonly ILogger<Worker> _logger;

    public Worker(
        KafkaConsumerFactory consumerFactory,
        IKafkaEventPublisher publisher,
        DeviceProjectionStore deviceProjectionStore,
        IOptions<KafkaOptions> kafkaOptions,
        ILogger<Worker> logger)
    {
        _consumerFactory = consumerFactory;
        _publisher = publisher;
        _deviceProjectionStore = deviceProjectionStore;
        _kafkaOptions = kafkaOptions.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var consumer = _consumerFactory.Create("decision-engine");
        consumer.Subscribe([
            _kafkaOptions.AlertsCreatedTopic,
            _kafkaOptions.DevicesRegisteredTopic,
            _kafkaOptions.DevicesStatusUpdatedTopic
        ]);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var result = consumer.Consume(stoppingToken);
                await HandleMessageAsync(result, stoppingToken);

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

    private async Task HandleMessageAsync(ConsumeResult<string, string> result, CancellationToken cancellationToken)
    {
        switch (result.Topic)
        {
            case var topic when topic == _kafkaOptions.DevicesRegisteredTopic:
                HandleDeviceRegistered(result.Message.Value);
                break;
            case var topic when topic == _kafkaOptions.DevicesStatusUpdatedTopic:
                HandleDeviceStatusUpdated(result.Message.Value);
                break;
            case var topic when topic == _kafkaOptions.AlertsCreatedTopic:
                await HandleAlertCreatedAsync(result.Message.Value, cancellationToken);
                break;
        }
    }

    private void HandleDeviceRegistered(string payload)
    {
        var deviceEvent = JsonMessageSerializer.Deserialize<DeviceRegisteredEvent>(payload);
        if (deviceEvent is null)
        {
            _logger.LogWarning("Skipping unreadable device registration event");
            return;
        }

        _deviceProjectionStore.Upsert(deviceEvent);
        _logger.LogInformation(
            "Registered device projection for user {UserId} device {DeviceId}",
            deviceEvent.UserId,
            deviceEvent.DeviceId);
    }

    private void HandleDeviceStatusUpdated(string payload)
    {
        var deviceEvent = JsonMessageSerializer.Deserialize<DeviceStatusUpdatedEvent>(payload);
        if (deviceEvent is null)
        {
            _logger.LogWarning("Skipping unreadable device status event");
            return;
        }

        _deviceProjectionStore.Update(deviceEvent);
        _logger.LogInformation(
            "Updated device projection status for device {DeviceId} online {IsOnline}",
            deviceEvent.DeviceId,
            deviceEvent.IsOnline);
    }

    private async Task HandleAlertCreatedAsync(string payload, CancellationToken cancellationToken)
    {
        var createdEvent = JsonMessageSerializer.Deserialize<AlertCreatedEvent>(payload);
        if (createdEvent is null)
        {
            _logger.LogWarning("Skipping unreadable alert event");
            return;
        }

        var targets = _deviceProjectionStore.GetTargets(createdEvent.TargetUserId, createdEvent.IsBroadcast);

        using (_logger.BeginScope(new Dictionary<string, object> { ["eventId"] = createdEvent.EventId }))
        {
            if (targets.Count == 0)
            {
                _logger.LogWarning(
                    "No registered devices found for target {TargetUserId} broadcast {IsBroadcast}",
                    createdEvent.TargetUserId,
                    createdEvent.IsBroadcast);

                await PublishFailureAsync(
                    createdEvent,
                    userId: createdEvent.TargetUserId,
                    deviceId: "unresolved",
                    failedChannel: Alerting.Shared.Enums.DispatchChannel.Email,
                    failureReason: "No registered devices available for target.",
                    cancellationToken);

                return;
            }

            foreach (var device in targets)
            {
                var channel = createdEvent.Priority.ResolveInitialChannel(
                    device.Channels,
                    device.IsOnline,
                    device.PushToken);

                if (channel is null)
                {
                    await PublishFailureAsync(
                        createdEvent,
                        device.UserId,
                        device.DeviceId,
                        Alerting.Shared.Enums.DispatchChannel.Email,
                        "No available channel matched the device capabilities.",
                        cancellationToken);
                    continue;
                }

                var processedEvent = new AlertProcessedEvent(
                    EventId: createdEvent.EventId,
                    UserId: device.UserId,
                    DeviceId: device.DeviceId,
                    Message: createdEvent.Message,
                    Priority: createdEvent.Priority,
                    Channel: channel.Value,
                    TimestampUtc: DateTime.UtcNow,
                    AvailableChannels: device.Channels,
                    IsOnline: device.IsOnline,
                    NetworkType: device.NetworkType,
                    PushToken: device.PushToken,
                    IsFallback: false,
                    PreviousChannel: null,
                    Source: createdEvent.Source,
                    OriginalFormat: createdEvent.OriginalFormat);

                _logger.LogInformation(
                    "Decision engine resolved alert for user {UserId} device {DeviceId} to channel {Channel}",
                    device.UserId,
                    device.DeviceId,
                    channel.Value);

                await _publisher.PublishAsync(
                    _kafkaOptions.AlertsProcessedTopic,
                    $"{createdEvent.EventId}:{device.DeviceId}:{channel.Value}",
                    processedEvent,
                    cancellationToken);
            }
        }
    }

    private Task PublishFailureAsync(
        AlertCreatedEvent createdEvent,
        string userId,
        string deviceId,
        Alerting.Shared.Enums.DispatchChannel failedChannel,
        string failureReason,
        CancellationToken cancellationToken)
    {
        var failedEvent = new AlertFailedEvent(
            EventId: createdEvent.EventId,
            UserId: userId,
            DeviceId: deviceId,
            Message: createdEvent.Message,
            Priority: createdEvent.Priority,
            FailedChannel: failedChannel,
            TimestampUtc: DateTime.UtcNow,
            FailureReason: failureReason);

        return _publisher.PublishAsync(
            _kafkaOptions.AlertsFailedTopic,
            $"{createdEvent.EventId}:{deviceId}:failed",
            failedEvent,
            cancellationToken);
    }
}
