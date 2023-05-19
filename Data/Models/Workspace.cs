using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace TaskManager.API.Data.Models
{
    public class Workspace
    {
        [Key]
        public int Id { get; set; }
        [Required, MaxLength(100)]
        public string Title { get; set; }
        public string? Description { get; set; }
        public string? Logo { get; set; }
        public string? Background { get; set; }
        [Required]
        public int Permission { get; set; }
        public string CreatorId { get; set; }
        public string CreatorName { get; set; }
        public DateTime? CreateAt { get; set; }
        public DateTime? UpdateAt { get; set; }

        public ICollection<Card> Cards { get; set; } = null;
        public ICollection<UserWorkspace> UserWorkspaces { get; set; } = null;
        public ICollection<Account> Users { get; set; }= null;
        public ICollection<Activation> Activations { get; set; }= null;
        public ICollection<Schedule> Schedules { get; set; }= null;
    }
}