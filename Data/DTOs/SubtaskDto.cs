using System.ComponentModel.DataAnnotations;

namespace TaskManager.API.Data.DTOs
{
    public class SubtaskDto
    {
        public int Id { get; internal set; }
        [Required, MaxLength(100)]
        public string Name { get; set; }
        public bool? Status { get; set; } = false;
        [Required]
        public int TaskItemId { get; set; }
    }
}