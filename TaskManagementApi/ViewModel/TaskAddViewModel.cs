namespace TaskManagementApi.ViewModel
{
    using System.ComponentModel.DataAnnotations;

    public sealed class TaskAddViewModel
    {
        [Required]
        public string? Title { get; set; }
    }
}
