
using System.ComponentModel.DataAnnotations;
using TaskManager.API.Data.Models;

namespace TaskManager.API.Data.DTOs
{
    public class TaskItemDto
    {
        public int Id { get; internal set; }
        [Required, MaxLength(50)]
        public string Title { get; set; }
        public string? Description { get; set; }
        public string? Attachment { get;}
        [Required]
        public PriorityEnum Priority { get; set; }
        // public DateTime? StartDate { get; set; }
        public DateTime? DueDate { get; set; }
        [Required]
        public int CardId { get; set; }

        public List<UserTaskDto> Comments { get; internal set; } = null;
        public List<UserTaskDto> Assigns { get; internal set; } = null;
        public List<LabelDto> Labels { get; internal set; } = null;
    }
}