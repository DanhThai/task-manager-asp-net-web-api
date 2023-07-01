

using System.ComponentModel.DataAnnotations;

namespace TaskManager.API.Data.DTOs
{
    public class UserUpdateDto
    {
        public string Email { get; set; }

        public string Password { get; set; }

        public string FullName { get; set; }
        public string Avatar { get; set; }
  
    }
}