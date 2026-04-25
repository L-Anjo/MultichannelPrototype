using Alerting.Shared.Configuration;
using Microsoft.Extensions.Options;

namespace Push.Service;

public sealed class PushDispatchSimulator
{
    private readonly DispatcherSimulationOptions _options;

    public PushDispatchSimulator(IOptions<DispatcherSimulationOptions> options)
    {
        _options = options.Value;
    }

    public bool ShouldFail() => Random.Shared.NextDouble() < _options.PushFailureRate;
}
