
using System.ComponentModel.DataAnnotations;

namespace TaskManager.API.Data.DTOs
{
    public class MemberTaskDto
    {
        public int Id { get; set;}
        public bool Requested { get; internal set; } = false;
        public DateTime? ExtendDate { get; set;}
        
        public string UserId { get; set;}
        public string FullName { get; internal set; }
        public string Avatar { get; internal set; } = null;
        public string Url { get => "/account/user/"+ UserId;}

        [Required]
        public int TaskItemId { get; set; }

    }
}