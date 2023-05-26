using System.ComponentModel.DataAnnotations;
using TaskManager.API.Data.Models;

namespace TaskManager.API.Data.DTOs
{
    public class MemberWorkspaceDto
    {
        public string Id { get; set;}
        [EmailAddress]
        public string Email { get; set;}
        public string FullName { get; internal set; }
        public string Avatar { get; internal set; } = null;
        public string Url { get => "/account/user/"+ Id;}
        public ROLE_ENUM Role {get; set; }
        public int TaskQuantity { get; internal set; } = 0;
        public int CompletedQuantity { get; internal set; } = 0;
        public int Percentage { get{

            if(TaskQuantity == 0)
                return 0;
            
            return (int)CompletedQuantity*100/TaskQuantity;
        }}

    }
}