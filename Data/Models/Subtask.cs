using System.ComponentModel.DataAnnotations;

namespace TaskManager.API.Data.Models
{
    public class Subtask
    {
        [Key]
        public int Id { get; set; }
        [Required, MaxLength(100)]
        public string Name { get; set; }
        public bool Status { get; set; } = false;
        public int ChecklistId { get; set; }
        public Checklist Checklist { get; set; }

    }
}