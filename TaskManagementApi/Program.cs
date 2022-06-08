using MessageQueue;

using Microsoft.EntityFrameworkCore;

using TaskManagementApi.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddSingleton(builder.Configuration.GetSection("RabbitMq").Get<RabbitMqSettings>());
builder.Services.AddSingleton<IMessageQueuePublisherService, RabbitMqMessageQueuePublisherService>();

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddDbContext<TaskDbContext>(options =>
    options.UseNpgsql("Host=postgres;Database=taskdb;Username=postgres;Password=postgres"));

builder.Services.AddCors(options =>
    {
        options.AddPolicy(name: "CorsOrigins",
            policy =>
                {
                    policy.WithOrigins("http://localhost:8080", "http://localhost:8083");
                });
    });

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("CorsOrigins");
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
