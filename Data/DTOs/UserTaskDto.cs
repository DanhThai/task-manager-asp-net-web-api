
namespace TaskManager.API.Data.DTOs
{
    public class UserTaskDto
    {
        public string Id { get; set;}
        public string Email { get; set;}
        public string FullName { get; set; }
        public string Avatar { get; set; } = null;
        public string Url { get => "/account/user/"+ Id;}
        public string Comment { get; set; }
        public bool Assigned { get; set; }
        public bool IsCreator { get; set; }
    }
}