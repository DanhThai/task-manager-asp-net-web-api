using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace TaskManager.API.Data.Models
{
    public class Label
    {
        [Key]
        public int Id { get; set; }
        [Required, MaxLength(50)]
        public string Name { get; set; }
        [Required, MaxLength(30)]
        public string Color { get; set; }
        public DateTime? CreateAt { get; set; }
        public int WorkspaceId { get; set; }
        public Workspace Workspace { get; set; }

        public ICollection<TaskItem> TaskItems { get; set; } = null;
        public ICollection<TaskLabel> TaskLabels { get; set; } = null;
    }
}