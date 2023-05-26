
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace TaskManager.API.Data.DTOs
{
    public class WorkspaceDto
    {
        [Key]
        public int? Id { get; internal set; }
        [Required, MaxLength(50)]
        public string Title { get; set; }
        public string? Description { get; set; }
        public string? Logo { get; internal set; }
        public string? Background { get; internal set; }
        [Required]
        public int Permission { get; set; }
        public bool? IsOwner { get; internal set;} = false;
        public string CreatorId { get; internal set;}
        public string CreatorName { get; internal set;}
        public List<MemberWorkspaceDto> Members {get; internal set;} = new List<MemberWorkspaceDto>();
        public List<CardDto> Cards {get; internal set;} = new List<CardDto>();
        public List<ActivationDto> Activations {get; internal set;} = new List<ActivationDto>();
        public List<ScheduleDto> Schedules {get; internal set;} = new List<ScheduleDto>();
    }
}