using Alerting.Shared.Enums;

namespace Alerting.Shared.Models;

public sealed record AlertProcessedEvent(
    Guid EventId,
    string UserId,
    string DeviceId,
    string Message,
    AlertPriority Priority,
    DispatchChannel Channel,
    DateTime TimestampUtc,
    IReadOnlyCollection<DispatchChannel> AvailableChannels,
    bool IsOnline,
    string NetworkType,
    string? PushToken,
    bool IsFallback,
    DispatchChannel? PreviousChannel,
    string Source,
    string OriginalFormat);
