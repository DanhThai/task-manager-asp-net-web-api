using System.ComponentModel.DataAnnotations;

namespace TaskManager.API.Data.Models
{
    public class UserTask
    {
        [Key]
        public int Id { get; set; }
        public string? Comment { get; set; }
        public bool Assigned { get; set; } = false;
        public bool IsCreator { get; set; } = false;
        public string UserId { get; set; }
        public int TaskItemId { get; set; }
        public Account User { get; set; }
        public TaskItem TaskItem { get; set; }
    }
}