using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;

namespace TaskManager.API.Data.Models
{
    public class Account: IdentityUser
    {
        [Required]
        public string FullName { get; set; }
        public string? Avatar { get; set; } = null;
        public string? ImageName { get; set; } = null;

        //  Relationships
        public ICollection<UserWorkspace> UserWorkspaces { get; set; } = null;
        public ICollection<Workspace> Workspaces { get; set;} = null;
        public ICollection<UserTask> UserTasks { get; set;} = null;
        public ICollection<TaskItem> TaskItems { get;  } = null;
        public ICollection<Activation> Activations { get;  } = null;
    }
}