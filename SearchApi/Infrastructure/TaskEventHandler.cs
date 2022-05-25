namespace SearchApi.Infrastructure
{
    using MessageQueue;

    using Nest;

    public class TaskEventHandler : BackgroundService
    {
        private readonly IMessageQueueConsumerService<Model.Task> _messageQueueConsumerService;

        private readonly IElasticClient _elasticClient;

        private readonly ILogger<TaskEventHandler> _logger;

        public TaskEventHandler(IMessageQueueConsumerService<Model.Task> messageQueueConsumerService, IElasticClient elasticClient, ILogger<TaskEventHandler> logger)
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
                    m =>
                        {
                            _logger.LogInformation($"Consuming task {m.Id}");

                            IndexResponse indexResponse;

                            try
                            {
                                indexResponse = _elasticClient.IndexDocument(m);
                            }
                            catch (System.Exception e)
                            {
                                 _logger.LogError(e, $"Index document error for task {m.Id}");
                                throw;
                            }

                            if (!indexResponse.IsValid || indexResponse.Result != Result.Created)
                            {
                                var errorMessage = $"{m.Id} task could not be indexed";
                                if (indexResponse.OriginalException != null)
                                {
                                    var ex1 = new InvalidOperationException(errorMessage, indexResponse.OriginalException);

                                    _logger.LogError(ex1, errorMessage);

                                    throw ex1;
                                }

                                var ex = new InvalidOperationException(errorMessage);

                                 _logger.LogError(ex, errorMessage);

                                throw ex;
                            }

                            _logger.LogInformation($"Index response for task {m.Id}: Result={indexResponse.Result}, Index={indexResponse.Index}");
                        });
            }

            return Task.CompletedTask;
        }
    }
}
