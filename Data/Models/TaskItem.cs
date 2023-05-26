
using System.ComponentModel.DataAnnotations;

namespace TaskManager.API.Data.Models
{
    public enum PRIORITY_ENUM
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
        public int CommentQuantity { get; set; } = 0;
        public int SubtaskQuantity { get; set; } = 0;
        public int SubtaskCompleted { get; set; } = 0;
        public bool IsComplete { get; set; } = false;
        [Required]
        public PRIORITY_ENUM Priority { get; set; }
        // public DateTime? StartDate { get; set; }
        public DateTime? DueDate { get; set; }
        public DateTime? CreatAt { get; set; }
        public DateTime? UpdateAt { get; set; }

        // Relationship
        public int CardId { get; set; }
        public Card Card { get; set; }
        public string CreatorId { get; set; }
        public Account Creator { get; set; }

        public ICollection<MemberTask> MemberTasks { get; set; }= null;
        // public ICollection<Account> Users { get; set; }= null;
        public ICollection<TaskLabel> TaskLabels { get; set; }= null;
        public ICollection<Label> Labels { get; set; }= null;
        public ICollection<Subtask> Subtasks { get; set; }= null;
        public ICollection<Comment> Comments { get; set; } = null;
      
    }
}