using System.ComponentModel.DataAnnotations;

namespace TaskManager.API.Data.DTOs
{
    public class CommentDto
    {
        public int Id { get; internal set; }
        public string Content { get; set; }
        public DateTime UpdateAt { get; internal set; }

        public string FullName { get; internal set; }
        public string Avatar { get; internal set; } = null;
        public string Url { get => "/account/user/"+ UserId;}
        public string? UserId { get; set; }
        [Required]
        public int TaskItemId { get; set; }
    }
}