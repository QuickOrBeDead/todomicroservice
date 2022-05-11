namespace TaskManagementApi.Controllers
{
    using MessageQueue;

    using Microsoft.AspNetCore.Mvc;

    using TaskManagementApi.Infrastructure;

    [ApiController]
    [Route("[controller]")]
    public class TaskController : ControllerBase
    {
        private readonly TaskDbContext _taskDbContext;

        private readonly IMessageQueueService _messageQueueService;

        public TaskController(TaskDbContext taskDbContext, IMessageQueueService messageQueueService)
        {
            _taskDbContext = taskDbContext;
            _messageQueueService = messageQueueService;
        }

        [HttpGet(Name = "GetTasks")]
        public IEnumerable<TaskEntity> Get()
        {
            return _taskDbContext.Tasks.ToList();
        }

        [HttpPost(Name = "AddTask")]
        public async Task Add(string title)
        {
            var taskEntity = new TaskEntity { Title = title };

            await _taskDbContext.Tasks.AddAsync(taskEntity).ConfigureAwait(false);
            await _taskDbContext.SaveChangesAsync().ConfigureAwait(false);

            _messageQueueService.PublishMessage(taskEntity);
        }
    }
}