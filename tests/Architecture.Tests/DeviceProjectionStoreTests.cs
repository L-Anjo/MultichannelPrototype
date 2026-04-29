using Alerting.Shared.Enums;
using Alerting.Shared.Models;
using Decision.Engine;

namespace Architecture.Tests;

public sealed class DeviceProjectionStoreTests
{
    [Fact]
    public void LoadSnapshotMakesDevicesAvailableForBroadcast()
    {
        var store = new DeviceProjectionStore();
        store.LoadSnapshot(
        [
            new RegisteredDevice(
                DeviceId: "device-1",
                UserId: "user-1",
                Channels: [DispatchChannel.Push],
                PushToken: "token-1",
                IsOnline: true,
                NetworkType: "wifi",
                LastUpdatedUtc: DateTime.UtcNow),
            new RegisteredDevice(
                DeviceId: "device-2",
                UserId: "user-2",
                Channels: [DispatchChannel.Sms],
                PushToken: null,
                IsOnline: false,
                NetworkType: "cellular",
                LastUpdatedUtc: DateTime.UtcNow)
        ]);

        var targets = store.GetTargets("broadcast", isBroadcast: true);

        Assert.Equal(2, targets.Count);
    }

    [Fact]
    public void UpdateCreatesUnknownDeviceWhenStatusArrivesFirst()
    {
        var store = new DeviceProjectionStore();

        store.Update(new DeviceStatusUpdatedEvent(
            DeviceId: "device-early",
            IsOnline: false,
            NetworkType: "cellular",
            TimestampUtc: DateTime.UtcNow));

        var broadcastTargets = store.GetTargets("broadcast", isBroadcast: true);
        var userTargets = store.GetTargets("unknown", isBroadcast: false);

        Assert.Empty(broadcastTargets);
        Assert.Single(userTargets);
        Assert.Equal("device-early", userTargets.First().DeviceId);
    }
}
