
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
        public string Content { get; set; }
        public string Color { get; set; }
        [Required]
        public DateTime Date { get; set; }
        [Required]
        public int WorkspaceId { get; set; }
    }
}