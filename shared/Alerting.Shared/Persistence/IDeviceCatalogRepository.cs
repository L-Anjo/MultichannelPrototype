using Alerting.Shared.Models;

namespace Alerting.Shared.Persistence;

public interface IDeviceCatalogRepository
{
    Task InitializeAsync(CancellationToken cancellationToken);
    Task<IReadOnlyCollection<RegisteredDevice>> GetAllAsync(CancellationToken cancellationToken);
    Task UpsertAsync(DeviceRegisteredEvent deviceEvent, CancellationToken cancellationToken);
    Task UpdateStatusAsync(DeviceStatusUpdatedEvent statusEvent, CancellationToken cancellationToken);
}
