using Alerting.Shared.Enums;

namespace Alerting.Shared.Models;

public sealed record DeviceRegistrationRequest(
    string DeviceId,
    string UserId,
    IReadOnlyCollection<DispatchChannel> Channels,
    string? PushToken,
    bool IsOnline,
    string NetworkType);
