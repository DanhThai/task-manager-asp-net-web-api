
using System.ComponentModel.DataAnnotations;

namespace TaskManager.API.Data.DTOs
{
    public class UserWorkspaceDto
    {
        public string Id { get; set;}
        [EmailAddress]
        public string Email { get; set;}
        public string FullName { get; set; }
        public string Avatar { get; set; }
        public string Role { get; set; }
        public bool EmailConfirmed { get; set; }
        public int WorkspaceQuantity { get; set; } = 0;
        public List<WorkspaceDto> Workspaces { get; set; }
    }
    
}
