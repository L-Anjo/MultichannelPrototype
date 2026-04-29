using Alerting.Shared.Extensions;
using Decision.Engine;

var builder = Host.CreateApplicationBuilder(args);

builder.AddStructuredConsoleLogging();
builder.Services.AddKafkaMessaging(builder.Configuration);
builder.Services.AddPostgresDeviceCatalog(builder.Configuration);
builder.Services.AddSingleton<KafkaConsumerFactory>();
builder.Services.AddSingleton<DeviceProjectionStore>();
builder.Services.AddHostedService<DeviceProjectionWarmupService>();
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
