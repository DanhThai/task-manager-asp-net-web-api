using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace TaskManager.API.Data.Models
{
    public class Activation
    {
        [Key]
        public int Id { get; set; }
        [Required, MaxLength(256)]
        public string Content { get; set; }
        public DateTime CreateAt { get; set; }
        [Required]
        public int WorkspaceId { get; set; }
        [Required]
        public string UserId { get; set; }
        public Workspace Workspace { get; set; }
        public Account User { get; set; }
    }
}