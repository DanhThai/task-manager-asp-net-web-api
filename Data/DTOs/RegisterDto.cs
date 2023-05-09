

using System.ComponentModel.DataAnnotations;

namespace TaskManager.API.Data.DTOs
{
    public class RegisterDto
    {
        [Required(ErrorMessage = "Email is required"), EmailAddress]
        public string Email { get; set; }

        [Required(ErrorMessage = "Password is required")]
        public string Password { get; set; }

        [Required]
        public string FullName { get; set; }
  
    }
}