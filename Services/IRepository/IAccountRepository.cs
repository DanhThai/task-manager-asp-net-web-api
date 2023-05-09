
using System.Security.Claims;
using TaskManager.API.Data.DTOs;

namespace TaskManager.API.Services.IRepository
{
    public interface IAccountRepository
    {
        public Task<Response> LogInAsync(LogInDto user);
        public Task<Response> RegisterAsync(RegisterDto user);
        public Task<Response> RegisterAdminAsync(RegisterDto user);

        public Task<Response> UploadAvatarAsync(ClaimsPrincipal user, IFormFile img);

        public Task<Response> ConfirmEmailAsync(string userId, string token);

        // public Task<Response> GetAccountByIdAsync(string userId);


    }
}