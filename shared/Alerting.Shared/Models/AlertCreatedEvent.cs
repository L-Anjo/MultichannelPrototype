using Alerting.Shared.Enums;

namespace Alerting.Shared.Models;

public sealed record AlertCreatedEvent(
    Guid EventId,
    string Source,
    string Message,
    AlertPriority Priority,
    DateTime TimestampUtc,
    string OriginalFormat,
    string CapIdentifier,
    string Sender,
    string Urgency,
    string Severity,
    string TargetUserId,
    bool IsBroadcast);
