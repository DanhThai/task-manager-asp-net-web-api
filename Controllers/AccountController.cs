
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using TaskManager.API.Data.DTOs;
using TaskManager.API.Services.IRepository;

namespace TaskManager.API.Controllers
{
    [ApiController]
    [Route("account")]
    public class AccountController : ControllerBase
    {
        private readonly IAccountRepository _accountRepository;
        public AccountController(IAccountRepository accountRepository)
        {
            _accountRepository = accountRepository;
        }
        
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> GetUser(){
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);    
            var result = await _accountRepository.GetUserAsync(userId);
            return Ok(result);
        }

        [HttpPost("login")]
        public async Task<IActionResult> LogIn(LogInDto user){
            var result = await _accountRepository.LogInAsync(user);
            return Ok(result);
        }

        [HttpPost("register")]
        public async Task<IActionResult> RegisterUser(RegisterDto user){
            var result = await _accountRepository.RegisterAsync(user);
            return Ok(result);
        }

        [HttpPost("register-admin")]
        public async Task<IActionResult> RegisterAdmin(RegisterDto user){
            
            var result = await _accountRepository.RegisterAdminAsync(user);
            return Ok(result);
        }

        [HttpGet("confirm-email")]
        public async Task<RedirectResult> ConfirmEmail(string userId, string token){
            if (userId == null || token == null){
                return RedirectPermanent("https://localhost:7070/ConfirmEmailError.html");
            }
            var result = await _accountRepository.ConfirmEmailAsync(userId, token);
            return RedirectPermanent(result.Message);;
        }

        [Authorize]
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateUser(string id, UserUpdateDto user){
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);    
            if (userId != id){
                return BadRequest();
            }
            var result = await _accountRepository.UpdateUserAsync(userId, user);
            return Ok(result);
        }
        
        [HttpPut("upload-avt")]
        public async Task<IActionResult> UploadAvatar(UserUpdateDto user){
            if(user.Avatar == "")
                return BadRequest();
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);            
            var rs = await _accountRepository.UploadAvatarAsync(userId, user.Avatar);
            return Ok(rs);
        }

        [HttpPost("admin/login")]
        public async Task<IActionResult> LogInAdmin(LogInDto admin){
            var result = await _accountRepository.LogInAdminAsync(admin);
            return Ok(result);
        }

        [Authorize]
        [HttpGet("admin/users")]
        public async Task<IActionResult> GetListUsers(){
            var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier);    
            var result = await _accountRepository.GetListUsersAsync(adminId);
            return Ok(result);
        }

        [Authorize]
        [HttpGet("admin/users/{userId}")]
        public async Task<IActionResult> GetListUsers(string userId){
            var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier);    
            var result = await _accountRepository.GetListWorkspacesByUserAsync(adminId, userId);
            return Ok(result);
        }

        
    }
}