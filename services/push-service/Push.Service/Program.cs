using Alerting.Shared.Configuration;
using Alerting.Shared.Extensions;
using Push.Service;

var builder = Host.CreateApplicationBuilder(args);

builder.AddStructuredConsoleLogging();
builder.Services.AddKafkaMessaging(builder.Configuration);
builder.Services.Configure<DispatcherSimulationOptions>(
    builder.Configuration.GetSection(DispatcherSimulationOptions.SectionName));
builder.Services.AddSingleton<KafkaConsumerFactory>();
builder.Services.AddSingleton<PushDispatchSimulator>();
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
