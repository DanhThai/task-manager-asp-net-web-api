
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace TaskManager.API.Data.DTOs
{
    public class WorkspaceDto
    {
        public int? Id { get; internal set; }
        [Required, MaxLength(50)]
        public string Title { get; set; }
        public string? Description { get; set; }
        public string? Logo { get; internal set; }
        public string? Background { get; internal set; }
        [Required]
        public int Permission { get; set; }
        public bool? IsOwner { get; internal set;}
        public string CreatorId { get; internal set;}
        public string CreatorName { get; internal set;}
        public List<UserDto> Members {get; internal set;} = new List<UserDto>();
        public List<CardDto> Cards {get; internal set;} = new List<CardDto>();
    }
}