using Alerting.Shared.Enums;

namespace Alerting.Shared.Models;

public sealed record AlertFailedEvent(
    Guid EventId,
    string UserId,
    string DeviceId,
    string Message,
    AlertPriority Priority,
    DispatchChannel FailedChannel,
    DateTime TimestampUtc,
    string FailureReason);
