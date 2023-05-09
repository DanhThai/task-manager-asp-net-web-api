using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace TaskManager.API.Data.Models
{
    public class UserWorkspace
    {
        [Key]
        public int Id { get; set; }
        public bool IsOwner { get; set; }
        public string UserId { get; set; }
        public int WorkspaceId { get; set; }
        public Account User { get; set; }
        public Workspace Workspace { get; set; }
    }
}