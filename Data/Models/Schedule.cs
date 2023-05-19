
using System.ComponentModel.DataAnnotations;

namespace TaskManager.API.Data.Models
{
    public class Schedule
    {
        [Key]
        public int Id { get; set; }
        [Required, MaxLength(256)]
        public string Title { get; set; }
        [MaxLength(256)]
        public string Content { get; set; }
        [MaxLength(20)]
        public string Color { get; set; }
        [Required]
        public DateTime Date { get; set; }
        public int WorkspaceId { get; set; }
        public Workspace Workspace { get; set; }

    }
}