
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
        public string Description { get; set; }
        [MaxLength(20)]
        public string Color { get; set; }
        [Required]
        public DateTime StartDateTime { get; set; }
        public DateTime EndDateTime { get; set; }
        public string CreatorId { get; set; }
        public Account Creator { get; set; }
        public int WorkspaceId { get; set; }
        public Workspace Workspace { get; set; }
    }
}