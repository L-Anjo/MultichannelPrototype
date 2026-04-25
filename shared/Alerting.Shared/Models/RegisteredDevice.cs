using Alerting.Shared.Enums;

namespace Alerting.Shared.Models;

public sealed record RegisteredDevice(
    string DeviceId,
    string UserId,
    IReadOnlyCollection<DispatchChannel> Channels,
    string? PushToken,
    bool IsOnline,
    string NetworkType,
    DateTime LastUpdatedUtc);
