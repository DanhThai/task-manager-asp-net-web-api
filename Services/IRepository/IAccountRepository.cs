
using System.Security.Claims;
using TaskManager.API.Data.DTOs;

namespace TaskManager.API.Services.IRepository
{
    public interface IAccountRepository
    {
        public Task<Response> LogInAsync(LogInDto user);
        public Task<Response> RegisterAsync(RegisterDto user);
        public Task<Response> RegisterAdminAsync(RegisterDto user);



        public Task<Response> ConfirmEmailAsync(string userId, string token);

        // public Task<Response> GetAccountByIdAsync(string userId);
        public Task<Response> GetUserAsync(string userId);

        public Task<Response> UpdateUserAsync(string userId, UserUpdateDto user);
        public  Task<Response> UploadAvatarAsync(string userId, string avatar_url);
        public Task<Response> LogInAdminAsync(LogInDto user);

        public Task<Response> GetListUsersAsync(string adminId);
        public Task<Response> GetListWorkspacesByUserAsync(string adminId, string userId);


    }
}