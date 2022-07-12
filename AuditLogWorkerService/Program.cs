using AuditLogWorkerService;
using AuditLogWorkerService.Infrastructure.Data;

using MessageQueue;
using MessageQueue.Events;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        services.AddSingleton<IRabbitMqConnection>(_ => new DefaultRabbitMqConnection(context.Configuration.GetSection("RabbitMqConnection").Get<RabbitMqConnectionSettings>()));
        services.AddSingleton<IMessageQueueConsumerService<EventBase>>(x => new RabbitMqMessageQueueConsumerService<EventBase>(x.GetRequiredService<IRabbitMqConnection>(), "taskmanagement.task.added"));

        services.AddSingleton(context.Configuration.GetSection("MongoDb").Get<MongoDbSettings>());
        services.AddSingleton<IRepository, MongoDbRepository>();

        services.AddHostedService<Worker>();
    })
    .Build();

await host.RunAsync();
