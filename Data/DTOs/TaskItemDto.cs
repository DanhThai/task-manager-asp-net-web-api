
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
        public PRIORITY_ENUM Priority { get; set; }
        // public DateTime? StartDate { get; set; }
        public DateTime? DueDate { get; set; }

        public int CommentQuantity { get; internal set; } = 0;
        public int SubtaskQuantity { get; internal set; } = 0;
        public int SubtaskCompleted { get; internal set; } = 0;
        public int Percentage { get{
            if(SubtaskQuantity == 0)
                return 0;
            
            return SubtaskCompleted*100/SubtaskQuantity;
        }}
        public bool IsComplete { get; internal set; } = false;

        [Required]
        public int CardId { get; set; }

        public string CreatorId { get; set; }
        public string FullName { get; internal set; }
        public string Email { get; internal set; }
        public string Avatar { get; internal set; }

        public MemberTaskDto? MemberExtendDueDate { get; internal set; }

        public List<MemberTaskDto> Members { get; internal set; } = new List<MemberTaskDto>();
        public List<LabelDto> Labels { get; internal set; } = new List<LabelDto>();
        public List<SubtaskDto> Subtasks { get; internal set; } = new List<SubtaskDto>();
        public ICollection<CommentDto> Comments { get; internal set; } = null;

    }
}