using Alerting.Shared.Enums;

namespace Alerting.Shared.Extensions;

public static class AlertRoutingExtensions
{
    public static AlertPriority ToPriority(this (string Urgency, string Severity) capInfo)
    {
        var urgency = capInfo.Urgency.Trim();
        var severity = capInfo.Severity.Trim();

        if (urgency.Equals("Immediate", StringComparison.OrdinalIgnoreCase) &&
            severity.Equals("Severe", StringComparison.OrdinalIgnoreCase))
        {
            return AlertPriority.High;
        }

        if (urgency.Equals("Expected", StringComparison.OrdinalIgnoreCase) &&
            severity.Equals("Moderate", StringComparison.OrdinalIgnoreCase))
        {
            return AlertPriority.Medium;
        }

        if (urgency.Equals("Future", StringComparison.OrdinalIgnoreCase) &&
            severity.Equals("Minor", StringComparison.OrdinalIgnoreCase))
        {
            return AlertPriority.Low;
        }

        return AlertPriority.Medium;
    }

    public static IReadOnlyCollection<DispatchChannel> ToChannels(this AlertPriority priority) =>
        priority switch
        {
            AlertPriority.High => [DispatchChannel.Push, DispatchChannel.Sms],
            AlertPriority.Medium => [DispatchChannel.Push],
            AlertPriority.Low => [DispatchChannel.Email],
            _ => [DispatchChannel.Push]
        };

    public static DispatchChannel? NextFallbackChannel(this DispatchChannel channel) =>
        channel switch
        {
            DispatchChannel.Push => DispatchChannel.Sms,
            DispatchChannel.Sms => DispatchChannel.Email,
            _ => null
        };
}
