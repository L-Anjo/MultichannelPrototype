using Alerting.Shared.Enums;

namespace Alerting.Shared.Models;

public sealed record AlertProcessedEvent(
    Guid EventId,
    string Message,
    AlertPriority Priority,
    DateTime TimestampUtc,
    IReadOnlyCollection<DispatchChannel> RequestedChannels,
    string Source,
    string OriginalFormat);
