namespace TaskManagementApi.Controllers
{
    using Microsoft.AspNetCore.Mvc;

    using TaskManagementApi.Infrastructure;

    [ApiController]
    [Route("[controller]")]
    public class TaskController : ControllerBase
    {
        private readonly TaskDbContext _taskDbContext;

        public TaskController(TaskDbContext taskDbContext)
        {
            _taskDbContext = taskDbContext;
        }

        [HttpGet(Name = "GetTasks")]
        public IEnumerable<TaskEntity> Get()
        {
            return _taskDbContext.Tasks.ToList();
        }

        [HttpPost(Name = "AddTask")]
        public async Task Add(string title)
        {
            await _taskDbContext.Tasks.AddAsync(new TaskEntity { Title = title }).ConfigureAwait(false);
            await _taskDbContext.SaveChangesAsync().ConfigureAwait(false);
        }
    }
}