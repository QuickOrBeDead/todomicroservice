using MessageQueue;

using UnroutedMessageService;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        services.AddSingleton(context.Configuration.GetSection("RabbitMq").Get<RabbitMqSettings>());
        services.AddSingleton<IMessageQueuePublisherService, RabbitMqMessageQueuePublisherService>();
        services.AddSingleton<IMessageQueueConsumerService, RabbitMqMessageQueueConsumerService>();

        services.AddHostedService<Worker>();
    })
    .Build();

await host.RunAsync();
