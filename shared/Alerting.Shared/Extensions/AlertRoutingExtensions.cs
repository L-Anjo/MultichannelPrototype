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

    public static DispatchChannel? ResolveInitialChannel(
        this AlertPriority priority,
        IReadOnlyCollection<DispatchChannel> channels,
        bool isOnline,
        string? pushToken)
    {
        var normalized = channels.ToHashSet();

        return priority switch
        {
            AlertPriority.High => ResolveHighPriorityChannel(normalized, isOnline, pushToken),
            AlertPriority.Medium => ResolveMediumPriorityChannel(normalized, isOnline, pushToken),
            AlertPriority.Low => ResolveLowPriorityChannel(normalized),
            _ => null
        };
    }

    public static bool SupportsChannel(this IReadOnlyCollection<DispatchChannel> channels, DispatchChannel channel) =>
        channels.Contains(channel);

    private static DispatchChannel? ResolveHighPriorityChannel(
        ISet<DispatchChannel> channels,
        bool isOnline,
        string? pushToken)
    {
        if (isOnline &&
            !string.IsNullOrWhiteSpace(pushToken) &&
            channels.Contains(DispatchChannel.Push))
        {
            return DispatchChannel.Push;
        }

        if (channels.Contains(DispatchChannel.Sms))
        {
            return DispatchChannel.Sms;
        }

        if (channels.Contains(DispatchChannel.Email))
        {
            return DispatchChannel.Email;
        }

        if (channels.Contains(DispatchChannel.Whatsapp))
        {
            return DispatchChannel.Whatsapp;
        }

        if (channels.Contains(DispatchChannel.Telegram))
        {
            return DispatchChannel.Telegram;
        }

        return null;
    }

    private static DispatchChannel? ResolveMediumPriorityChannel(
        ISet<DispatchChannel> channels,
        bool isOnline,
        string? pushToken)
    {
        if (isOnline &&
            !string.IsNullOrWhiteSpace(pushToken) &&
            channels.Contains(DispatchChannel.Push))
        {
            return DispatchChannel.Push;
        }

        if (channels.Contains(DispatchChannel.Whatsapp))
        {
            return DispatchChannel.Whatsapp;
        }

        if (channels.Contains(DispatchChannel.Telegram))
        {
            return DispatchChannel.Telegram;
        }

        return null;
    }

    private static DispatchChannel? ResolveLowPriorityChannel(ISet<DispatchChannel> channels)
    {
        if (channels.Contains(DispatchChannel.Email))
        {
            return DispatchChannel.Email;
        }

        if (channels.Contains(DispatchChannel.Telegram))
        {
            return DispatchChannel.Telegram;
        }

        if (channels.Contains(DispatchChannel.Whatsapp))
        {
            return DispatchChannel.Whatsapp;
        }

        return null;
    }
}
