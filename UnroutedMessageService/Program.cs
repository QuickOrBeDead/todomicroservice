using MessageQueue;

using UnroutedMessageService;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices(services =>
    {
        services.AddSingleton<IMessageQueuePublisherService>(x => new RabbitMqMessageQueuePublisherService("rabbitmq", "guest", "guest", "Todo"));
        services.AddSingleton<IMessageQueueConsumerService>(x => new RabbitMqMessageQueueConsumerService("rabbitmq", "guest", "guest", "Todo", "unrouted", false));

        services.AddHostedService<Worker>();
    })
    .Build();

await host.RunAsync();
