using Alerting.Shared.Enums;
using Alerting.Shared.Models;
using Device.Service;

namespace Architecture.Tests;

public sealed class DeviceStoreTests
{
    [Fact]
    public void LoadSnapshotReplacesCurrentInMemoryState()
    {
        var store = new DeviceStore();

        store.Upsert(new DeviceRegisteredEvent(
            DeviceId: "old-device",
            UserId: "old-user",
            Channels: [DispatchChannel.Email],
            PushToken: null,
            IsOnline: false,
            NetworkType: "cellular",
            TimestampUtc: DateTime.UtcNow));

        store.LoadSnapshot(
        [
            new RegisteredDevice(
                DeviceId: "fresh-device",
                UserId: "fresh-user",
                Channels: [DispatchChannel.Push, DispatchChannel.Sms],
                PushToken: "token",
                IsOnline: true,
                NetworkType: "wifi",
                LastUpdatedUtc: DateTime.UtcNow)
        ]);

        Assert.Equal(1, store.Count);
    }
}
