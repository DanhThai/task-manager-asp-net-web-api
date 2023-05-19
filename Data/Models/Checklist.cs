using System.ComponentModel.DataAnnotations;

namespace TaskManager.API.Data.Models
{
    public class Checklist
    {
        [Required, MaxLength(50)]
        public string Name { get; set; }
        public bool Status { get; set; } = false;
        // public int SubtaskQuantity { get; set; } = 0;
        // public int SubtaskCompleted { get; set; } = 0;
        [Key]
        public int Id { get; set; }
        public TaskItem TaskItem { get; set; }
        public ICollection<Subtask> Subtasks { get; set; }
    }
}