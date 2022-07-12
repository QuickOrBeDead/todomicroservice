namespace SearchApi.Infrastructure
{
    using MessageQueue;

    using Nest;

    using SearchApi.Events;

    public class TaskEventHandler : BackgroundService
    {
        private readonly IMessageQueueConsumerService<TaskAddedEvent> _messageQueueConsumerService;

        private readonly IElasticClient _elasticClient;

        private readonly ILogger<TaskEventHandler> _logger;

        public TaskEventHandler(IMessageQueueConsumerService<TaskAddedEvent> messageQueueConsumerService, IElasticClient elasticClient, ILogger<TaskEventHandler> logger)
        {
            _messageQueueConsumerService = messageQueueConsumerService ?? throw new ArgumentNullException(nameof(messageQueueConsumerService));
            _elasticClient = elasticClient ?? throw new ArgumentNullException(nameof(elasticClient));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (!stoppingToken.IsCancellationRequested)
            {
                _elasticClient.Indices.Create(
                    $"task-{DateTime.UtcNow:dd-MM-yyyy-HH-mm-ss}",
                    x => x.Map<Model.Task>(m => m.AutoMap()));

                _messageQueueConsumerService.ConsumeMessage(
                    (m, _) =>
                        {
                            _logger.LogInformation($"Consuming task {m.Id}");

                            IndexResponse indexResponse;

                            try
                            {
                                indexResponse = _elasticClient.IndexDocument(new Model.Task
                                                                                 {
                                                                                     Id = m.TaskId,
                                                                                     Title = m.Title
                                                                                 });
                            }
                            catch (Exception e)
                            {
                                 _logger.LogError(e, $"Index document error for task {m.Id}");
                                throw;
                            }

                            if (!indexResponse.IsValid || indexResponse.Result != Result.Created)
                            {
                                var errorMessage = $"{m.Id} task could not be indexed";
                                if (indexResponse.OriginalException != null)
                                {
                                    LogAndThrowException(new InvalidOperationException(errorMessage, indexResponse.OriginalException));
                                }

                                LogAndThrowException(new InvalidOperationException(errorMessage));
                            }

                            _logger.LogInformation($"Index response for task {m.Id}: Result={indexResponse.Result}, Index={indexResponse.Index}");

                            return true;
                        });
            }

            return Task.CompletedTask;
        }

        private void LogAndThrowException(Exception ex)
        {
            _logger.LogError(ex, ex.Message);

            throw ex;
        }
    }
}
