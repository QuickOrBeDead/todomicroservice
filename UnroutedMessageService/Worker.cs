namespace UnroutedMessageService
{
    using MessageQueue;

    public class Worker : BackgroundService
    {
        private readonly IMessageQueueConsumerService _messageQueueConsumerService;

        private readonly IMessageQueuePublisherService _messageQueuePublisherService;

        public Worker(IMessageQueueConsumerService messageQueueConsumerService, IMessageQueuePublisherService messageQueuePublisherService)
        {
            _messageQueueConsumerService = messageQueueConsumerService;
            _messageQueuePublisherService = messageQueuePublisherService;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            stoppingToken.Register(() => { _messageQueueConsumerService.Dispose(); });

            if (!stoppingToken.IsCancellationRequested)
            {
                _messageQueueConsumerService.ConsumeMessage((m, t) =>
                        {
                            Thread.Sleep(10_000);
                            _messageQueuePublisherService.Publish(m, t);
                        });
            }

            return Task.CompletedTask;
        }
    }
}