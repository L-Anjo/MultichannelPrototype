namespace Alerting.Shared.Models;

public sealed record DeviceStatusUpdateRequest(
    bool IsOnline,
    string NetworkType);
