
using System.ComponentModel.DataAnnotations;

namespace TaskManager.API.Data.DTOs
{
    public class LabelDto
    {
        [Key]
        public int Id { get; set; }
        [Required, MaxLength(50)]
        public string Name { get; set; }
        [Required, MaxLength(30)]
        public string Color { get; set; }
        [Required]
        public int WorkspaceId { get; set; }
    }
}