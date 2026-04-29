using Alerting.Shared.Enums;
using Alerting.Shared.Extensions;

namespace Architecture.Tests;

public sealed class AlertRoutingExtensionsTests
{
    [Fact]
    public void HighPriorityPrefersPushWhenDeviceIsOnlineAndHasToken()
    {
        var channel = AlertPriority.High.ResolveInitialChannel(
            [DispatchChannel.Push, DispatchChannel.Sms, DispatchChannel.Email],
            isOnline: true,
            pushToken: "token-123");

        Assert.Equal(DispatchChannel.Push, channel);
    }

    [Fact]
    public void HighPriorityFallsBackToSmsWhenPushIsUnavailable()
    {
        var channel = AlertPriority.High.ResolveInitialChannel(
            [DispatchChannel.Push, DispatchChannel.Sms, DispatchChannel.Email],
            isOnline: false,
            pushToken: null);

        Assert.Equal(DispatchChannel.Sms, channel);
    }

    [Fact]
    public void MediumPriorityUsesOttWhenPushIsUnavailable()
    {
        var channel = AlertPriority.Medium.ResolveInitialChannel(
            [DispatchChannel.Whatsapp, DispatchChannel.Telegram],
            isOnline: true,
            pushToken: null);

        Assert.Equal(DispatchChannel.Whatsapp, channel);
    }

    [Fact]
    public void FallbackChainStopsAfterEmail()
    {
        Assert.Equal(DispatchChannel.Sms, DispatchChannel.Push.NextFallbackChannel());
        Assert.Equal(DispatchChannel.Email, DispatchChannel.Sms.NextFallbackChannel());
        Assert.Null(DispatchChannel.Email.NextFallbackChannel());
    }
}
