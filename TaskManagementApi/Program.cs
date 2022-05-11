using MessageQueue;

using Microsoft.EntityFrameworkCore;

using TaskManagementApi.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddSingleton<IMessageQueueService>(x => new RabbitMqMessageQueuePublisherService("rabbitmq", "guest", "guest", "Todo"));

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddDbContext<TaskDbContext>(options =>
    options.UseNpgsql("Host=postgres;Database=taskdb;Username=postgres;Password=postgres"));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();

app.MapControllers();

using (var serviceScope = app.Services
           .GetRequiredService<IServiceScopeFactory>()
           .CreateScope())
{
    using (var context = serviceScope.ServiceProvider.GetRequiredService<TaskDbContext>())
    {
        context.Database.Migrate();
    }
}


app.Run();
