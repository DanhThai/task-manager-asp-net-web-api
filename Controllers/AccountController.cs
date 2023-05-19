
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
        

        [HttpPost("login")]
        public async Task<IActionResult> LogIn(LogInDto user){
            var result = await _accountRepository.LogInAsync(user);
            return Ok(result);
        }

        [HttpPost("register")]
        public async Task<IActionResult> RegisterUser(RegisterDto user){
            var l = new List<int>(){1,2,3, 4, 5, 6, 7, 8, 9, 10, 11};
            string rs = JsonConvert.SerializeObject(l);
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
                return RedirectPermanent("https://localhost:7070/ConfirmEmailError.html");;
            }
            var result = await _accountRepository.ConfirmEmailAsync(userId, token);
            return RedirectPermanent(result.Message);;
        }
        
        [HttpPost("upload-avt")]
        public async Task<IActionResult> UploadAvatar([FromForm] IFormFile avatar){
            var rs = await _accountRepository.UploadAvatarAsync(User, avatar);
            return Ok(rs);
        }

        
    }
}