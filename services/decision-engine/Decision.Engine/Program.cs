using Alerting.Shared.Extensions;
using Decision.Engine;

var builder = Host.CreateApplicationBuilder(args);

builder.AddStructuredConsoleLogging();
builder.Services.AddKafkaMessaging(builder.Configuration);
builder.Services.AddSingleton<KafkaConsumerFactory>();
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
