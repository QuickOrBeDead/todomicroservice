using MessageQueue;

using Nest;

using SearchApi.Events;
using SearchApi.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddHostedService<TaskEventHandler>();
builder.Services.AddSingleton<IElasticClient>(_ => new ElasticClient(new ConnectionSettings(new Uri("http://elasticsearch:9200")).BasicAuthentication("elastic", "Password1").DefaultIndex("task").DefaultMappingFor<SearchApi.Infrastructure.Model.Task>(x => x.IdProperty(y => y.Id).IndexName("task"))));
builder.Services.AddSingleton<IRabbitMqConnection>(_ => new DefaultRabbitMqConnection(builder.Configuration.GetSection("RabbitMqConnection").Get<RabbitMqConnectionSettings>()));
builder.Services.AddSingleton<IMessageQueueConsumerService<TaskAddedEvent>>(x => new RabbitMqMessageQueueConsumerService<TaskAddedEvent>(x.GetRequiredService<IRabbitMqConnection>(), builder.Configuration.GetSection("RabbitMqConsumerTaskAdded").Get<RabbitMqConsumerSettings>()));
builder.Services.AddSingleton<IMessageQueueConsumerService<TaskStatusChangedEvent>>(x => new RabbitMqMessageQueueConsumerService<TaskStatusChangedEvent>(x.GetRequiredService<IRabbitMqConnection>(), builder.Configuration.GetSection("RabbitMqConsumerTaskStatusChanged").Get<RabbitMqConsumerSettings>()));

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();

app.MapControllers();

app.Run();
