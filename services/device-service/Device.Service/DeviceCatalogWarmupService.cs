using Alerting.Shared.Persistence;

namespace Device.Service;

public sealed class DeviceCatalogWarmupService : IHostedService
{
    private readonly IDeviceCatalogRepository _repository;
    private readonly DeviceStore _deviceStore;
    private readonly ILogger<DeviceCatalogWarmupService> _logger;

    public DeviceCatalogWarmupService(
        IDeviceCatalogRepository repository,
        DeviceStore deviceStore,
        ILogger<DeviceCatalogWarmupService> logger)
    {
        _repository = repository;
        _deviceStore = deviceStore;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await _repository.InitializeAsync(cancellationToken);
        var devices = await _repository.GetAllAsync(cancellationToken);
        _deviceStore.LoadSnapshot(devices);

        _logger.LogInformation(
            "Device service warmed local store from PostgreSQL with {Count} devices",
            devices.Count);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
