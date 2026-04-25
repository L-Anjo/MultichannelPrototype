using Alerting.Shared.Enums;

namespace Alerting.Shared.Models;

public sealed record AlertDispatchResultEvent(
    Guid EventId,
    string UserId,
    string DeviceId,
    string Message,
    AlertPriority Priority,
    DispatchChannel Channel,
    DispatchStatus Status,
    int Attempts,
    DateTime TimestampUtc,
    IReadOnlyCollection<DispatchChannel> AvailableChannels,
    bool IsFallback,
    DispatchChannel? PreviousChannel,
    string? PushToken,
    string? FailureReason = null);
