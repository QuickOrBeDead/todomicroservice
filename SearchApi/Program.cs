using MessageQueue;

using Microsoft.OpenApi.Models;

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
builder.Services.AddSingleton<IMessageQueueConsumerService<TaskUpdatedEvent>>(x => new RabbitMqMessageQueueConsumerService<TaskUpdatedEvent>(x.GetRequiredService<IRabbitMqConnection>(), builder.Configuration.GetSection("RabbitMqConsumerTaskUpdated").Get<RabbitMqConsumerSettings>()));
builder.Services.AddSingleton<IMessageQueueConsumerService<TaskDeletedEvent>>(x => new RabbitMqMessageQueueConsumerService<TaskDeletedEvent>(x.GetRequiredService<IRabbitMqConnection>(), builder.Configuration.GetSection("RabbitMqConsumerTaskDeleted").Get<RabbitMqConsumerSettings>()));

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(options =>
    {
        options.AddPolicy(name: "CorsOrigins",
            policy =>
                {
                    policy.WithOrigins("http://localhost:8080", "http://localhost:8084").AllowAnyMethod().AllowAnyHeader();
                });
    });

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger(config =>
        {
            config.PreSerializeFilters.Add((document, request) =>
                {
                    var externalPath = !request.Headers.ContainsKey("x-envoy-original-path") ? string.Empty :
                                           request.Headers["x-envoy-original-path"].First().Replace("swagger/v1/swagger.json", string.Empty);
                    if (!string.IsNullOrWhiteSpace(externalPath))
                    {
                        var newPaths = new OpenApiPaths();
                        foreach (var path in document.Paths)
                        {
                            newPaths[$"{externalPath.TrimEnd('/')}{path.Key}"] = path.Value;
                        }

                        document.Paths = newPaths;
                    }
                });
        });
    app.UseSwaggerUI();
}

app.UseCors("CorsOrigins");
app.UseAuthorization();

app.MapControllers();

app.Run();
