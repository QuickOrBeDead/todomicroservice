namespace AuditLogWorkerService
{
    using System.Text;

    using AuditLogWorkerService.Infrastructure.Data;

    using MessageQueue;

    using MongoDB.Bson;
    using MongoDB.Bson.Serialization;

    public class Worker : BackgroundService
    {
        private readonly IMessageQueueConsumerService _messageQueueConsumerService;

        private readonly IRepository _repository;

        public Worker(IMessageQueueConsumerService messageQueueConsumerService, IRepository repository)
        {
            _messageQueueConsumerService = messageQueueConsumerService ?? throw new ArgumentNullException(nameof(messageQueueConsumerService));
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (!stoppingToken.IsCancellationRequested)
            {
                _messageQueueConsumerService.ConsumeMessage(
                    (m, t) =>
                        {
                            _repository.Insert(t, BsonSerializer.Deserialize<BsonDocument>(Encoding.UTF8.GetString(m)), cancellationToken: stoppingToken);

                            return true;
                        });
            }

            return Task.CompletedTask;
        }
    }
}