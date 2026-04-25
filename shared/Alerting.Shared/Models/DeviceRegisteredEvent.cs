using Alerting.Shared.Enums;

namespace Alerting.Shared.Models;

public sealed record DeviceRegisteredEvent(
    string DeviceId,
    string UserId,
    IReadOnlyCollection<DispatchChannel> Channels,
    string? PushToken,
    bool IsOnline,
    string NetworkType,
    DateTime TimestampUtc);
