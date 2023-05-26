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
        public int TaskItemId { get; set; }
        public TaskItem TaskItem { get; set; }
        public string? MemberId{ get; set; }
        public Account? AssignedMember { get; set; }

    }
}