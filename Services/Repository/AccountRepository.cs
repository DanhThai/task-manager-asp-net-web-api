
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using TaskManager.API.Data.DTOs;
using TaskManager.API.Data.Models;
using TaskManager.API.Services.IRepository;
using Microsoft.AspNetCore.WebUtilities;
using AutoMapper;

namespace TaskManager.API.Services.Repository
{
    public class AccountRepository : IAccountRepository
    {
        private readonly UserManager<Account> _userManager;
        private readonly SignInManager<Account> _signInManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IConfiguration _configuration;
        private readonly IWebService _webService;
        private readonly IMapper _mapper;


        public AccountRepository(SignInManager<Account> signInManager,
                                 UserManager<Account> userManager,
                                 RoleManager<IdentityRole> roleManager,
                                 IConfiguration configuration,
                                 IWebService webService,
                                 IMapper mapper)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _roleManager = roleManager;
            _configuration = configuration;
            _webService = webService;
            _mapper = mapper;
        }

        private JwtSecurityToken GetToken(List<Claim> authClaims)
        {
            var authSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JWT:SecretKey"]));

            var token = new JwtSecurityToken(
                issuer: _configuration["JWT:ValidIssuer"],
                audience: _configuration["JWT:ValidAudience"],
                expires: DateTime.Now.AddHours(5),
                claims: authClaims,
                signingCredentials: new SigningCredentials(authSigningKey, SecurityAlgorithms.HmacSha256)
                );

            return token;
        }

        public async Task<Response> LogInAsync(LogInDto user)
        {
            var userExist = await _userManager.FindByEmailAsync(user.Email);
            if (userExist != null && await _userManager.CheckPasswordAsync(userExist, user.Password))
            {
                if (userExist.EmailConfirmed == false)
                {
                    return new Response
                    {
                        Message = "Email is not confirmed.",
                        IsSuccess = false
                    };
                }
                var userRoles = await _userManager.GetRolesAsync(userExist);

                var authClaims = new List<Claim>
                {
                    new Claim("Name", userExist.FullName),
                    new Claim(ClaimTypes.NameIdentifier, userExist.Id),
                    new Claim("Role", userRoles[0]),
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                };
                var token = GetToken(authClaims);

                var userDto = _mapper.Map<Account, UserDto>(userExist); 
                var data = new Dictionary<string, object>
                {
                    ["token"] = new JwtSecurityTokenHandler().WriteToken(token),
                    ["account"] =  userDto
                };
                return new Response
                {
                    Message = "Login successful",
                    Data = data,
                    IsSuccess = true
                };
            }
            else
            {
                return new Response
                {
                    Message = "Email or Password are not true.",
                    IsSuccess = false
                };
            }
        }

        public async Task<Response> RegisterAsync(RegisterDto user)
        {
            try
            {
                var userCheck = await _userManager.FindByEmailAsync(user.Email);
                if (!await _roleManager.RoleExistsAsync("ADMIN"))
                {
                    await _roleManager.CreateAsync(new IdentityRole("ADMIN"));
                    await _roleManager.CreateAsync(new IdentityRole("USER"));
                }
                
                if (userCheck == null)
                {
                    Account account = new Account
                    {
                        FullName = user.FullName,           
                        Email = user.Email,
                        UserName = user.Email
                    };
                    var userCreated = await _userManager.CreateAsync(account, user.Password);
                    await _userManager.AddToRoleAsync(account, "USER");

                    if (userCreated.Succeeded)
                    {
                        // Send email confirm to user
                        var confirmToken = await _userManager.GenerateEmailConfirmationTokenAsync(account);
                        var encodedToken = Encoding.UTF8.GetBytes(confirmToken);
                        var validToken = WebEncoders.Base64UrlEncode(encodedToken);

                        var url = $"{_configuration["RootUrl"]}account/confirm-email?userId={account.Id}&token={validToken}";

                        #region html content send email confirmation
                        var body = "<div style=\"width:100%; height:100vh; background-color: #d0e7fb; display: flex; align-items: center; justify-content: center; margin:auto; box-sizing: border-box;\" >"
                                + "<div style =\"font-family: 'Lobster', cursive; margin:auto; border-radius: 4px;padding: 40px;min-width: 200px;max-width: 40%; background-color: azure;\" >"
                                + "<p style=\"margin: 0; padding: 10px 0 ;font-size: 2rem;width: 100%;text-align:left\">Account Verification</p>"
                                + "<p style=\"margin: 0;width:100%;padding: 10px 0;text-align: left;color: #7e7b7b;\">Please confirm your email address by clicking the link below so you can access to <span style=\"color:#447eb0\" >Task Tracking</span> system account.</p>"
                                + $"<a href=\"{url}\" style=\" padding: 10px 20px; background-color: #439b73;color: #ffff;text-decoration:none;border-radius: 3px;font-weight:500;display:block; text-align:center; margin-top:10px;margin-bottom:10px;\" >Verify your email address</a>"
                                + "</div> </div>";
                        #endregion

                        var email = new EmailOption
                        {
                            ToEmail = account.Email,
                            Subject = "Email confirmation",
                            Body = body
                        };
                        var send = await _webService.SendEmail(email);

                        // if(send){
                        //     return new Response{
                        //         Message = "Create Account successfully. Please confirm your account we've just sent your email.",
                        //         IsSuccess = true
                        //     };
                        // }

                        return new Response
                        {
                            Message = "Create Account successfully. Please confirm your account we've just sent your email.",
                            IsSuccess = false
                        };

                    }
                    return new Response
                    {
                        Message = "User is not created",
                        IsSuccess = false
                    };
                }
                else
                {
                    return new Response
                    {
                        Message = "Email is invalid",
                        IsSuccess = false
                    };
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception: register" + ex.Message);
                return new Response
                {
                    Message = "Can not create Account",
                    IsSuccess = true
                };
                // throw new NotImplementedException();
            }
        }
        public async Task<Response> RegisterAdminAsync(RegisterDto user)
        {
            try
            {
                var userCheck = await _userManager.FindByEmailAsync(user.Email);
                if (userCheck == null)
                {
                    var account = new Account
                    {
                        Email = user.Email,
                        UserName = user.Email,
                        FullName = user.FullName,
                    };
                    var rs = await _userManager.CreateAsync(account, user.Password);
                    await _userManager.AddToRoleAsync(account, "ADMIN");

                    if (rs.Succeeded)
                    {
                        // Send email confirm to user
                        var token = _userManager.GenerateEmailConfirmationTokenAsync(account);
                        var body = "";
                        var email = new EmailOption
                        {
                            ToEmail = account.Email,
                            Subject = "Email confirmation",
                            Body = body
                        };
                        var send = await _webService.SendEmail(email);

                        // if(send){
                        //     return new Response{
                        //         Message = "Create Account successfully",
                        //         IsSuccess = true
                        //     };
                        // }

                        return new Response
                        {
                            Message = "Email is invalid",
                            IsSuccess = false
                        };

                    }
                    return new Response
                    {
                        Message = "User is not created",
                        IsSuccess = false
                    };
                }
                else
                {
                    return new Response
                    {
                        Message = "Email is valid",
                        IsSuccess = false
                    };
                }
            }
            catch
            {
                throw new NotImplementedException();
            }
        }
        public async Task<Response> ConfirmEmailAsync(string userId, string token)
        {
            try
            {
                var userExist = await _userManager.FindByIdAsync(userId);
                if (userExist != null)
                {
                    // decode token
                    var decodedToken = WebEncoders.Base64UrlDecode(token);
                    string normalToken = Encoding.UTF8.GetString(decodedToken);

                    var confirmEmail = await _userManager.ConfirmEmailAsync(userExist, normalToken);
                    if (confirmEmail.Succeeded)
                    {
                        return new Response
                        {
                            Message = $"{_configuration["RootUrl"]}ConfirmEmail.html",
                            IsSuccess = true
                        };
                    }
                    return new Response
                    {
                        Message = "Email confirmation is failed",
                        IsSuccess = false
                    };
                }
                return new Response
                {
                    Message = "User does not exist",
                    IsSuccess = false
                };
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        public async Task<Response> UploadAvatarAsync(ClaimsPrincipal user, IFormFile img)
        {
            try{
                FileStream fs;
                FileStream ms = null;
                if (img.Length > 0)
                {
                    string path = "./FileUpload/Avatar/";
                    try
                    {
                        using (fs = new FileStream(Path.Combine(path, "avatar.jpg"), FileMode.Create))
                        {
                            await img.CopyToAsync(fs);
                        }
                        ms = new FileStream(Path.Combine(path, "avatar.jpg"), FileMode.Open);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }

                    var imgUrl = await _webService.UploadFileToFirebase(ms, "Avatar", img.FileName);

                    // Update avatar to db
                    var userCheck = await _userManager.GetUserAsync(user);
                    userCheck.Avatar = imgUrl;
                    await _userManager.UpdateAsync(userCheck);

                    return new Response
                    {
                        Message = "Upload avatar successfully",
                        Data = new Dictionary<string, object>
                        {
                            ["imageUrl"] = imgUrl
                        },
                        IsSuccess = true
                    };
                }
                return new Response
                {
                    Message = "Avatar is not uploaded",
                    IsSuccess = false
                };
            }
            catch (Exception e){
                throw e;
            }
        }
    }
}