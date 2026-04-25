using Alerting.Shared.Enums;

namespace Alerting.Shared.Models;

public sealed record AlertDispatchResultEvent(
    Guid EventId,
    string Message,
    AlertPriority Priority,
    DispatchChannel Channel,
    DispatchStatus Status,
    int Attempts,
    DateTime TimestampUtc,
    IReadOnlyCollection<DispatchChannel> RequestedChannels,
    string? FailureReason = null);
