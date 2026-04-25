using Alerting.Shared.Configuration;
using Alerting.Shared.Messaging;
using Alerting.Shared.Models;
using Confluent.Kafka;
using Microsoft.Extensions.Options;

namespace Device.Service;

public sealed class Worker : BackgroundService
{
    private readonly KafkaConsumerFactory _consumerFactory;
    private readonly DeviceStore _deviceStore;
    private readonly KafkaOptions _kafkaOptions;
    private readonly ILogger<Worker> _logger;

    public Worker(
        KafkaConsumerFactory consumerFactory,
        DeviceStore deviceStore,
        IOptions<KafkaOptions> kafkaOptions,
        ILogger<Worker> logger)
    {
        _consumerFactory = consumerFactory;
        _deviceStore = deviceStore;
        _kafkaOptions = kafkaOptions.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var consumer = _consumerFactory.Create("device-service");
        consumer.Subscribe([_kafkaOptions.DevicesRegisteredTopic, _kafkaOptions.DevicesStatusUpdatedTopic]);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var result = consumer.Consume(stoppingToken);

                switch (result.Topic)
                {
                    case var topic when topic == _kafkaOptions.DevicesRegisteredTopic:
                        HandleRegistered(result.Message.Value);
                        break;
                    case var topic when topic == _kafkaOptions.DevicesStatusUpdatedTopic:
                        HandleStatusUpdated(result.Message.Value);
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
                _logger.LogError(exception, "Kafka consume error in device service");
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Unexpected error in device service");
                await Task.Delay(1000, stoppingToken);
            }
        }
    }

    private void HandleRegistered(string payload)
    {
        var deviceEvent = JsonMessageSerializer.Deserialize<DeviceRegisteredEvent>(payload);
        if (deviceEvent is null)
        {
            return;
        }

        _deviceStore.Upsert(deviceEvent);
        _logger.LogInformation(
            "Device service stored device {DeviceId} for user {UserId}. Total devices={Count}",
            deviceEvent.DeviceId,
            deviceEvent.UserId,
            _deviceStore.Count);
    }

    private void HandleStatusUpdated(string payload)
    {
        var statusEvent = JsonMessageSerializer.Deserialize<DeviceStatusUpdatedEvent>(payload);
        if (statusEvent is null)
        {
            return;
        }

        _deviceStore.UpdateStatus(statusEvent);
        _logger.LogInformation(
            "Device service updated status for device {DeviceId} online={IsOnline}. Total devices={Count}",
            statusEvent.DeviceId,
            statusEvent.IsOnline,
            _deviceStore.Count);
    }
}
