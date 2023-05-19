
using System.ComponentModel.DataAnnotations;

namespace TaskManager.API.Data.Models
{
    public enum PriorityEnum: byte
    {
        Low = 0,
        Medium = 1,
        High = 2,
    }
    public class TaskItem
    {
        [Key]
        public int Id { get; set; }
        [Required, MaxLength(50)]
        public string Title { get; set; }
        public string? Description { get; set; }
        public string? Attachment { get; set; }
        public string? FileName { get; set; }
        public int SubtaskQuantity { get; set; } = 0;
        public int SubtaskCompleted { get; set; } = 0;
        [Required]
        public PriorityEnum Priority { get; set; }
        // public DateTime? StartDate { get; set; }
        public DateTime? DueDate { get; set; }
        public DateTime? CreatAt { get; set; }
        public DateTime? UpdateAt { get; set; }

        // Relationship
        public int CardId { get; set; }
        public Card Card { get; set; }
        public Checklist? Checklist { get; set; }

        public ICollection<UserTask> UserTasks { get; set; }= null;
        public ICollection<Account> Users { get; set; }= null;
        public ICollection<TaskLabel> TaskLabels { get; set; }= null;
        public ICollection<Label> Labels { get; set; }= null;
      
    }
}