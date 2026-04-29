using Alerting.Shared.Persistence;

namespace Decision.Engine;

public sealed class DeviceProjectionWarmupService : IHostedService
{
    private readonly IDeviceCatalogRepository _repository;
    private readonly DeviceProjectionStore _projectionStore;
    private readonly ILogger<DeviceProjectionWarmupService> _logger;

    public DeviceProjectionWarmupService(
        IDeviceCatalogRepository repository,
        DeviceProjectionStore projectionStore,
        ILogger<DeviceProjectionWarmupService> logger)
    {
        _repository = repository;
        _projectionStore = projectionStore;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await _repository.InitializeAsync(cancellationToken);
        var devices = await _repository.GetAllAsync(cancellationToken);
        _projectionStore.LoadSnapshot(devices);

        _logger.LogInformation(
            "Decision engine warmed device projection from PostgreSQL with {Count} devices",
            devices.Count);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
