using Alerting.Shared.Configuration;
using Alerting.Shared.Enums;
using Microsoft.Extensions.Options;

namespace Sms.Service;

public sealed class ChannelDispatchSimulator
{
    private readonly DispatcherSimulationOptions _options;

    public ChannelDispatchSimulator(IOptions<DispatcherSimulationOptions> options)
    {
        _options = options.Value;
    }

    public bool ShouldFail(DispatchChannel channel) =>
        channel switch
        {
            DispatchChannel.Sms => Random.Shared.NextDouble() < _options.SmsFailureRate,
            DispatchChannel.Email => Random.Shared.NextDouble() < _options.EmailFailureRate,
            _ => false
        };
}
