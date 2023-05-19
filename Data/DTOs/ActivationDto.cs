using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TaskManager.API.Data.DTOs
{
    public class ActivationDto
    {
        public string Content { get; set; }
        public DateTime CreateAt { get; set; }
        public int WorkspaceId { get; set; }
        public string UserId { get; set; }
        public string FullName { get; set; }
        public string Avatar { get; set; }
        public string Url { get => "/account/user/"+ UserId;}
        
    }
}