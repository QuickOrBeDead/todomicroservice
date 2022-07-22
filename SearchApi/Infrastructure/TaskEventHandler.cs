namespace SearchApi.Infrastructure;

using MessageQueue;

using Nest;

using SearchApi.Events;

public class TaskEventHandler : BackgroundService
{
    private readonly IMessageQueueConsumerService<TaskAddedEvent> _taskAddedConsumerService;

    private readonly IMessageQueueConsumerService<TaskStatusChangedEvent> _taskStatusChangedConsumerService;

    private readonly IElasticClient _elasticClient;

    private readonly ILogger<TaskEventHandler> _logger;

    public TaskEventHandler(
        IMessageQueueConsumerService<TaskAddedEvent> taskAddedConsumerService, 
        IMessageQueueConsumerService<TaskStatusChangedEvent> taskStatusChangedConsumerService,
        IElasticClient elasticClient, 
        ILogger<TaskEventHandler> logger)
    {
        _taskAddedConsumerService = taskAddedConsumerService ?? throw new ArgumentNullException(nameof(taskAddedConsumerService));
        _taskStatusChangedConsumerService = taskStatusChangedConsumerService ?? throw new ArgumentNullException(nameof(taskStatusChangedConsumerService));
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

            _taskAddedConsumerService.ConsumeMessage(
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

            _taskStatusChangedConsumerService.ConsumeMessage(
                (m, _) =>
                    {
                        var documentExistsResponse = _elasticClient.Get<Model.Task>(m.TaskId);
                        if (!documentExistsResponse.IsValid || !documentExistsResponse.Found)
                        {
                            var errorMessage = $"{m.Id} task could not be found";
                            if (documentExistsResponse.OriginalException != null)
                            {
                                LogAndThrowException(new InvalidOperationException(errorMessage, documentExistsResponse.OriginalException));
                            }

                            LogAndThrowException(new InvalidOperationException(errorMessage));

                            return false;
                        }

                        var task = documentExistsResponse.Source;
                        task.Completed = m.Completed;
                        var documentUpdateResponse = _elasticClient.Update<Model.Task>(m.TaskId, x => x.Doc(task));

                        if (!documentUpdateResponse.IsValid || (documentUpdateResponse.Result != Result.Updated && documentUpdateResponse.Result != Result.Noop))
                        {
                            var errorMessage = $"{m.Id} task could not be updated";
                            if (documentUpdateResponse.OriginalException != null)
                            {
                                LogAndThrowException(new InvalidOperationException(errorMessage, documentUpdateResponse.OriginalException));
                            }

                            LogAndThrowException(new InvalidOperationException(errorMessage));

                            return false;
                        }

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