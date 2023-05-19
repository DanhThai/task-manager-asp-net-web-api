using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace TaskManager.API.Data.DTOs
{
    public class ChecklistDto
    {
        [Key, Required]
        public int Id { get; set; }
        [Required, MaxLength(100)]
        public string Name { get; set; }
        public bool Status { get; internal set; } = false;
        public List<SubtaskDto> Subtasks { get; internal set; } = new List<SubtaskDto>();
    }
}