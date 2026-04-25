using System.Collections.Concurrent;
using Alerting.Shared.Models;

namespace Decision.Engine;

public sealed class DeviceProjectionStore
{
    private readonly ConcurrentDictionary<string, RegisteredDevice> _devices = new();

    public void Upsert(DeviceRegisteredEvent deviceEvent)
    {
        var device = new RegisteredDevice(
            DeviceId: deviceEvent.DeviceId,
            UserId: deviceEvent.UserId,
            Channels: deviceEvent.Channels,
            PushToken: deviceEvent.PushToken,
            IsOnline: deviceEvent.IsOnline,
            NetworkType: deviceEvent.NetworkType,
            LastUpdatedUtc: deviceEvent.TimestampUtc);

        _devices[deviceEvent.DeviceId] = device;
    }

    public void Update(DeviceStatusUpdatedEvent deviceEvent)
    {
        _devices.AddOrUpdate(
            deviceEvent.DeviceId,
            _ => new RegisteredDevice(
                DeviceId: deviceEvent.DeviceId,
                UserId: "unknown",
                Channels: Array.Empty<Alerting.Shared.Enums.DispatchChannel>(),
                PushToken: null,
                IsOnline: deviceEvent.IsOnline,
                NetworkType: deviceEvent.NetworkType,
                LastUpdatedUtc: deviceEvent.TimestampUtc),
            (_, current) => current with
            {
                IsOnline = deviceEvent.IsOnline,
                NetworkType = deviceEvent.NetworkType,
                LastUpdatedUtc = deviceEvent.TimestampUtc
            });
    }

    public IReadOnlyCollection<RegisteredDevice> GetTargets(string userId, bool isBroadcast)
    {
        if (isBroadcast)
        {
            return _devices.Values
                .Where(device => device.UserId != "unknown")
                .OrderBy(device => device.UserId)
                .ThenBy(device => device.DeviceId)
                .ToArray();
        }

        return _devices.Values
            .Where(device => string.Equals(device.UserId, userId, StringComparison.OrdinalIgnoreCase))
            .OrderBy(device => device.DeviceId)
            .ToArray();
    }
}
