using MessageQueue;

using Nest;

using SearchApi.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddHostedService<TaskEventHandler>();
builder.Services.AddSingleton<IElasticClient>(_ => new ElasticClient(new ConnectionSettings(new Uri("http://elasticsearch:9200")).BasicAuthentication("elastic", "Password1").DefaultIndex("task").DefaultMappingFor<SearchApi.Infrastructure.Model.Task>(x => x.IndexName("task"))));
builder.Services.AddSingleton<IRabbitMqConnection>(_ => new DefaultRabbitMqConnection(builder.Configuration.GetSection("RabbitMq").Get<RabbitMqConnectionSettings>()));
builder.Services.AddSingleton(typeof(IMessageQueueConsumerService<>), typeof(RabbitMqMessageQueueConsumerService<>));

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
