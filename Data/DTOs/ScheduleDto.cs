
using System.ComponentModel.DataAnnotations;

namespace TaskManager.API.Data.DTOs
{
    public class ScheduleDto
    {
        [Key]
        public int Id { get; set; }
        [Required, MaxLength(50)]
        public string Title { get; set; }
        [MaxLength(256)]
        public string Description { get; set; }
        public string Color { get; set; }
        public DateTime StartDateTime { get; set; }
        public DateTime EndDateTime { get; set; }
        [Required]
        public int WorkspaceId { get; set; }
        public string CreatorId { get; internal set; }
        public string FullName { get; internal set; }
        public string Email { get; internal set; }
        public string Avatar { get;internal set; }
    }
}