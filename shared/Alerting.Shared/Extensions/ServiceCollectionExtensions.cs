using Alerting.Shared.Configuration;
using Alerting.Shared.Messaging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Alerting.Shared.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddKafkaMessaging(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<KafkaOptions>(configuration.GetSection(KafkaOptions.SectionName));
        services.AddSingleton<IKafkaEventPublisher, KafkaEventPublisher>();
        return services;
    }
}
