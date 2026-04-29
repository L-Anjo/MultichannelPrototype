using Alerting.Shared.Configuration;
using Alerting.Shared.Messaging;
using Alerting.Shared.Models;
using Alerting.Shared.Persistence;
using Confluent.Kafka;
using Microsoft.Extensions.Options;

namespace Device.Service;

public sealed class Worker : BackgroundService
{
    private readonly KafkaConsumerFactory _consumerFactory;
    private readonly DeviceStore _deviceStore;
    private readonly IDeviceCatalogRepository _repository;
    private readonly KafkaOptions _kafkaOptions;
    private readonly ILogger<Worker> _logger;

    public Worker(
        KafkaConsumerFactory consumerFactory,
        DeviceStore deviceStore,
        IDeviceCatalogRepository repository,
        IOptions<KafkaOptions> kafkaOptions,
        ILogger<Worker> logger)
    {
        _consumerFactory = consumerFactory;
        _deviceStore = deviceStore;
        _repository = repository;
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
                        await HandleRegisteredAsync(result.Message.Value, stoppingToken);
                        break;
                    case var topic when topic == _kafkaOptions.DevicesStatusUpdatedTopic:
                        await HandleStatusUpdatedAsync(result.Message.Value, stoppingToken);
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

    private async Task HandleRegisteredAsync(string payload, CancellationToken cancellationToken)
    {
        var deviceEvent = JsonMessageSerializer.Deserialize<DeviceRegisteredEvent>(payload);
        if (deviceEvent is null)
        {
            _logger.LogWarning("Skipping unreadable device registration event");
            return;
        }

        _deviceStore.Upsert(deviceEvent);
        await _repository.UpsertAsync(deviceEvent, cancellationToken);
        _logger.LogInformation(
            "Device service stored device {DeviceId} for user {UserId}. Total devices={Count}",
            deviceEvent.DeviceId,
            deviceEvent.UserId,
            _deviceStore.Count);
    }

    private async Task HandleStatusUpdatedAsync(string payload, CancellationToken cancellationToken)
    {
        var statusEvent = JsonMessageSerializer.Deserialize<DeviceStatusUpdatedEvent>(payload);
        if (statusEvent is null)
        {
            _logger.LogWarning("Skipping unreadable device status event");
            return;
        }

        _deviceStore.UpdateStatus(statusEvent);
        await _repository.UpdateStatusAsync(statusEvent, cancellationToken);
        _logger.LogInformation(
            "Device service updated status for device {DeviceId} online={IsOnline}. Total devices={Count}",
            statusEvent.DeviceId,
            statusEvent.IsOnline,
            _deviceStore.Count);
    }
}
