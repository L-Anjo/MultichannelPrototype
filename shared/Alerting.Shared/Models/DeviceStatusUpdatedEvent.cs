namespace Alerting.Shared.Models;

public sealed record DeviceStatusUpdatedEvent(
    string DeviceId,
    bool IsOnline,
    string NetworkType,
    DateTime TimestampUtc);
