using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Alerting.Shared.Extensions;

public static class HostBuilderExtensions
{
    public static void AddStructuredConsoleLogging(this IHostApplicationBuilder builder)
    {
        builder.Logging.ClearProviders();
        builder.Logging.AddJsonConsole(options =>
        {
            options.IncludeScopes = true;
            options.TimestampFormat = "yyyy-MM-ddTHH:mm:ss.fffZ";
            options.UseUtcTimestamp = true;
        });
    }
}
