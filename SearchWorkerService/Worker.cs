namespace SearchWorkerService;

using System.Reflection;

using MessageQueue;

using Nest;

using SearchWorkerService.Events;

public class Worker : BackgroundService
{
    private readonly IMessageQueueConsumerService<TaskAddedEvent> _taskAddedConsumerService;

    private readonly IMessageQueueConsumerService<TaskStatusChangedEvent> _taskStatusChangedConsumerService;

    private readonly IMessageQueueConsumerService<TaskUpdatedEvent> _taskUpdatedConsumerService;

    private readonly IMessageQueueConsumerService<TaskDeletedEvent> _taskDeletedConsumerService;

    private readonly IMessageQueuePublisherService _messageQueuePublisherService;

    private readonly IElasticClient _elasticClient;

    private readonly ILogger<Worker> _logger;

    public Worker(
        IMessageQueueConsumerService<TaskAddedEvent> taskAddedConsumerService,
        IMessageQueueConsumerService<TaskStatusChangedEvent> taskStatusChangedConsumerService,
        IMessageQueueConsumerService<TaskUpdatedEvent> taskUpdatedConsumerService,
        IMessageQueueConsumerService<TaskDeletedEvent> taskDeletedConsumerService,
        IMessageQueuePublisherService messageQueuePublisherService,
        IElasticClient elasticClient,
        ILogger<Worker> logger)
    {
        _taskAddedConsumerService = taskAddedConsumerService ?? throw new ArgumentNullException(nameof(taskAddedConsumerService));
        _taskStatusChangedConsumerService = taskStatusChangedConsumerService ?? throw new ArgumentNullException(nameof(taskStatusChangedConsumerService));
        _taskUpdatedConsumerService = taskUpdatedConsumerService ?? throw new ArgumentNullException(nameof(taskUpdatedConsumerService));
        _taskDeletedConsumerService = taskDeletedConsumerService ?? throw new ArgumentNullException(nameof(taskDeletedConsumerService));
        _messageQueuePublisherService = messageQueuePublisherService ?? throw new ArgumentNullException(nameof(messageQueuePublisherService));
        _elasticClient = elasticClient ?? throw new ArgumentNullException(nameof(elasticClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!stoppingToken.IsCancellationRequested)
        {
            _elasticClient.Indices.Create(
                $"task-{DateTime.UtcNow:dd-MM-yyyy-HH-mm-ss}",
                x => x.Map<Infrastructure.Model.Task>(m => m.AutoMap()));

            _taskAddedConsumerService.ConsumeMessage(
                (m, _) =>
                {
                    _logger.LogInformation($"Consuming task {m.Id}");

                    IndexResponse indexResponse;

                    try
                    {
                        indexResponse = _elasticClient.IndexDocument(new Infrastructure.Model.Task
                        {
                            Id = m.TaskId,
                            Title = m.Title
                        });
                    }
                    catch (Exception e)
                    {
                        _logger.LogError(e, $"Index document error for task {m.Id}");
                        _messageQueuePublisherService.PublishMessage(new GeneralNotificationEvent(GetGeneralNotificationMessage(m.TaskId, "could not be added to ElasticSearch")));
                        throw;
                    }

                    if (!indexResponse.IsValid || indexResponse.Result != Result.Created)
                    {
                        _messageQueuePublisherService.PublishMessage(new GeneralNotificationEvent(GetGeneralNotificationMessage(m.TaskId, "could not be added to ElasticSearch")));

                        var errorMessage = $"{m.Id} task could not be indexed";
                        if (indexResponse.OriginalException != null)
                        {
                            LogAndThrowException(new InvalidOperationException(errorMessage, indexResponse.OriginalException));
                        }

                        LogAndThrowException(new InvalidOperationException(errorMessage));
                    }

                    _logger.LogInformation($"Index response for task {m.Id}: Result={indexResponse.Result}, Index={indexResponse.Index}");

                    _messageQueuePublisherService.PublishMessage(new GeneralNotificationEvent(GetGeneralNotificationMessage(m.TaskId, "is added to ElasticSearch")));

                    return true;
                });

            _taskStatusChangedConsumerService.ConsumeMessage(
                (m, _) =>
                {
                    var documentExistsResponse = _elasticClient.Get<Infrastructure.Model.Task >(m.TaskId);
                    if (!documentExistsResponse.IsValid || !documentExistsResponse.Found)
                    {
                        _messageQueuePublisherService.PublishMessage(new GeneralNotificationEvent(GetGeneralNotificationMessage(m.TaskId, "could not be found on ElasticSearch")));

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
                    var documentUpdateResponse = _elasticClient.Update<Infrastructure.Model.Task>(m.TaskId, x => x.Doc(task));

                    if (!documentUpdateResponse.IsValid || (documentUpdateResponse.Result != Result.Updated && documentUpdateResponse.Result != Result.Noop))
                    {
                        _messageQueuePublisherService.PublishMessage(new GeneralNotificationEvent(GetGeneralNotificationMessage(m.TaskId, "could not be updated on ElasticSearch")));

                        var errorMessage = $"{m.Id} task could not be updated";
                        if (documentUpdateResponse.OriginalException != null)
                        {
                            LogAndThrowException(new InvalidOperationException(errorMessage, documentUpdateResponse.OriginalException));
                        }

                        LogAndThrowException(new InvalidOperationException(errorMessage));

                        return false;
                    }

                    _messageQueuePublisherService.PublishMessage(new GeneralNotificationEvent(GetGeneralNotificationMessage(m.TaskId, $"Completed property is set to {m.Completed} on ElasticSearch")));

                    return true;
                });

            _taskUpdatedConsumerService.ConsumeMessage(
                (m, _) =>
                {
                    var documentExistsResponse = _elasticClient.Get< Infrastructure.Model.Task >(m.TaskId);
                    if (!documentExistsResponse.IsValid || !documentExistsResponse.Found)
                    {
                        _messageQueuePublisherService.PublishMessage(new GeneralNotificationEvent(GetGeneralNotificationMessage(m.TaskId, "could not be found on ElasticSearch")));

                        var errorMessage = $"{m.Id} task could not be found";
                        if (documentExistsResponse.OriginalException != null)
                        {
                            LogAndThrowException(new InvalidOperationException(errorMessage, documentExistsResponse.OriginalException));
                        }

                        LogAndThrowException(new InvalidOperationException(errorMessage));

                        return false;
                    }

                    var task = documentExistsResponse.Source;
                    task.Title = m.Title;
                    var documentUpdateResponse = _elasticClient.Update<Infrastructure.Model.Task>(m.TaskId, x => x.Doc(task));

                    if (!documentUpdateResponse.IsValid || (documentUpdateResponse.Result != Result.Updated && documentUpdateResponse.Result != Result.Noop))
                    {
                        _messageQueuePublisherService.PublishMessage(new GeneralNotificationEvent(GetGeneralNotificationMessage(m.TaskId, "could not be updated on ElasticSearch")));

                        var errorMessage = $"{m.Id} task could not be updated";
                        if (documentUpdateResponse.OriginalException != null)
                        {
                            LogAndThrowException(new InvalidOperationException(errorMessage, documentUpdateResponse.OriginalException));
                        }

                        LogAndThrowException(new InvalidOperationException(errorMessage));

                        return false;
                    }

                    _messageQueuePublisherService.PublishMessage(new GeneralNotificationEvent(GetGeneralNotificationMessage(m.TaskId, "is updated on ElasticSearch")));

                    return true;
                });

            _taskDeletedConsumerService.ConsumeMessage(
                (m, _) =>
                {
                    var documentDeleteResponse = _elasticClient.Delete<Infrastructure.Model.Task >(m.TaskId);
                    if (documentDeleteResponse.IsValid && documentDeleteResponse.Result != Result.Error)
                    {
                        _messageQueuePublisherService.PublishMessage(new GeneralNotificationEvent(GetGeneralNotificationMessage(m.TaskId, "is deleted on ElasticSearch")));

                        return true;
                    }

                    _messageQueuePublisherService.PublishMessage(new GeneralNotificationEvent(GetGeneralNotificationMessage(m.TaskId, "could not be deleted on ElasticSearch")));

                    var errorMessage = $"{m.Id} task could not be deleted";
                    if (documentDeleteResponse.OriginalException != null)
                    {
                        LogAndThrowException(new InvalidOperationException(errorMessage, documentDeleteResponse.OriginalException));
                    }

                    LogAndThrowException(new InvalidOperationException(errorMessage));

                    return false;
                });
        }

        return Task.CompletedTask;
    }

    private static string GetGeneralNotificationMessage(int taskId, string action)
    {
        return $"Task with id {taskId} {action} by {Assembly.GetExecutingAssembly().GetName().Name}";
    }

    private void LogAndThrowException(Exception ex)
    {
        _logger.LogError(ex, ex.Message);

        throw ex;
    }
}