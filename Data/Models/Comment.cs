using System.ComponentModel.DataAnnotations;

namespace TaskManager.API.Data.Models
{
    public class Comment
    {
        [Key]
        public int Id { get; set; }
        public string? Content { get; set; }
        public DateTime CreateAt { get; set; }
        public DateTime UpdateAt { get; set; }
        public string UserId { get; set; }
        public int TaskItemId { get; set; }
        public Account User { get; set; }
        public TaskItem TaskItem { get; set; }
    }
}