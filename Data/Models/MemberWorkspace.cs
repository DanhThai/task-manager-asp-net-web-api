using System.ComponentModel.DataAnnotations;

namespace TaskManager.API.Data.Models
{
    public enum ROLE_ENUM{
        Owner = 0,
        Admin = 1,
        Member = 2,
    }
    public class MemberWorkspace
    {
        [Key]
        public int Id { get; set; }
        public ROLE_ENUM Role { get; set; }
        public DateTime VisitDate { get; set; }
        public string UserId { get; set; }
        public int WorkspaceId { get; set; }
        public Account User { get; set; }
        public Workspace Workspace { get; set; }
    }
}