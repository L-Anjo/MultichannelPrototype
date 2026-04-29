using System.Collections.Concurrent;
using Alerting.Shared.Models;

namespace Device.Service;

public sealed class DeviceStore
{
    private readonly ConcurrentDictionary<string, RegisteredDevice> _devices = new();

    public void LoadSnapshot(IEnumerable<RegisteredDevice> devices)
    {
        _devices.Clear();

        foreach (var device in devices)
        {
            _devices[device.DeviceId] = device;
        }
    }

    public void Upsert(DeviceRegisteredEvent deviceEvent)
    {
        _devices[deviceEvent.DeviceId] = new RegisteredDevice(
            DeviceId: deviceEvent.DeviceId,
            UserId: deviceEvent.UserId,
            Channels: deviceEvent.Channels,
            PushToken: deviceEvent.PushToken,
            IsOnline: deviceEvent.IsOnline,
            NetworkType: deviceEvent.NetworkType,
            LastUpdatedUtc: deviceEvent.TimestampUtc);
    }

    public void UpdateStatus(DeviceStatusUpdatedEvent statusEvent)
    {
        _devices.AddOrUpdate(
            statusEvent.DeviceId,
            _ => new RegisteredDevice(
                DeviceId: statusEvent.DeviceId,
                UserId: "unknown",
                Channels: Array.Empty<Alerting.Shared.Enums.DispatchChannel>(),
                PushToken: null,
                IsOnline: statusEvent.IsOnline,
                NetworkType: statusEvent.NetworkType,
                LastUpdatedUtc: statusEvent.TimestampUtc),
            (_, current) => current with
            {
                IsOnline = statusEvent.IsOnline,
                NetworkType = statusEvent.NetworkType,
                LastUpdatedUtc = statusEvent.TimestampUtc
            });
    }

    public int Count => _devices.Count;
}
