
using System.ComponentModel.DataAnnotations;
using TaskManager.API.Data.Models;

namespace TaskManager.API.Data.DTOs
{
    public class TaskItemDto
    {
        public int Id { get; internal set; }
        [Required, MaxLength(50)]
        public string Title { get; set; }
        public int SubtaskQuantity { get; internal set; } = 0;
        public int SubtaskCompleted { get; internal set; } = 0;
        public string? Description { get; set; }
        public string? Attachment { get;}
        [Required]
        public PriorityEnum Priority { get; set; }
        // public DateTime? StartDate { get; set; }
        public DateTime? DueDate { get; set; }
        [Required]
        public int CardId { get; set; }
        public ChecklistDto? Checklist { get; internal set; } = null;

        public List<UserTaskDto> Comments { get; internal set; } = new List<UserTaskDto>();
        public List<UserTaskDto> Assigns { get; internal set; } = new List<UserTaskDto>();
        public List<LabelDto> Labels { get; internal set; } = new List<LabelDto>();
    }
}