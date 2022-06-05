using AuditLogWorkerService;
using AuditLogWorkerService.Infrastructure.Data;

using MessageQueue;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        services.AddSingleton(context.Configuration.GetSection("RabbitMq").Get<RabbitMqSettings>());
        services.AddSingleton<IMessageQueueConsumerService, RabbitMqMessageQueueConsumerService>();

        services.AddSingleton(context.Configuration.GetSection("MongoDb").Get<MongoDbSettings>());
        services.AddSingleton<IRepository, MongoDbRepository>();

        services.AddHostedService<Worker>();
    })
    .Build();

await host.RunAsync();
