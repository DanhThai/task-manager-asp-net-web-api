
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
using TaskManager.API.Data;
using Dapper;
using System.Data;

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
        private readonly DapperContext _dapperContext;



        public AccountRepository(SignInManager<Account> signInManager,
                                 UserManager<Account> userManager,
                                 RoleManager<IdentityRole> roleManager,
                                 IConfiguration configuration,
                                 IWebService webService,
                                 IMapper mapper,
                                 DapperContext dapperContext)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _roleManager = roleManager;
            _configuration = configuration;
            _webService = webService;
            _mapper = mapper;
            _dapperContext = dapperContext;
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
                        Message = "Email chưa được xác thực.",
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
                    Message = "Đăng nhập thành công.",
                    Data = data,
                    IsSuccess = true
                };
            }
            else
            {
                return new Response
                {
                    Message = "Email hoặc mật khẩu không đúng.",
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
                            Message = "Tạo tài khoản thành công. Hãy xác nhận tài khoản qua email.",
                            IsSuccess = true
                        };

                    }
                    return new Response
                    {
                        Message = "Tài khoản không tạo được",
                        IsSuccess = false
                    };
                }
                else
                {
                    return new Response
                    {
                        Message = "Email đã tồn tại, bạn hãy nhập email khác.",
                        IsSuccess = false
                    };
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception: register" + ex.Message);
                return new Response
                {
                    Message = $"Đã có lỗi xảy ra: {ex.Message} .",
                    IsSuccess = false
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
                        EmailConfirmed = true
                    };
                    var rs = await _userManager.CreateAsync(account, user.Password);
                    await _userManager.AddToRoleAsync(account, "ADMIN");

                    if (rs.Succeeded)
                    {
                       
                        return new Response
                        {
                            Message = "Tạo tài khoản quản trị viên thành công",
                            IsSuccess = true
                        };

                    }
                    return new Response
                    {
                        Message = "Tạo tài khoản quản trị viên thất bại",
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
                        Message = "Xác thực email bị lỗi",
                        IsSuccess = false
                    };
                }
                return new Response
                {
                    Message = "Tài khoản không tồn tại",
                    IsSuccess = false
                };
            }
            catch (Exception e)
            {
                throw e;
            }
        }
 
        public async Task<Response> LogInAdminAsync(LogInDto user)
        {
            var userExist = await _userManager.FindByEmailAsync(user.Email);
            if (userExist != null && await _userManager.CheckPasswordAsync(userExist, user.Password))
            {
                var userRoles = await _userManager.GetRolesAsync(userExist);
                if (userRoles[0] != "ADMIN")
                {
                    return new Response
                    {
                        Message = "Bạn không có quyền đăng nhập vào hệ thống.",
                        IsSuccess = false
                    };
                }

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
                    Message = "Đăng nhập thành công.",
                    Data = data,
                    IsSuccess = true
                };
            }
            else
            {
                return new Response
                {
                    Message = "Email hoặc mật khẩu không đúng.",
                    IsSuccess = false
                };
            }
        }

        public async Task<Response> GetUserAsync(string userId)
        {
            try
            {
                var userCheck = await _userManager.FindByIdAsync(userId);
                if (userCheck != null)
                {
                    var user = _mapper.Map<Account, UserDto>(userCheck);
      
                    return new Response
                    {
                        Message = "Lấy thông tin tài khoản thành công",
                        Data = new Dictionary<string, object>{
                            ["user"] = user
                        },
                        IsSuccess = true
                    };
                }
                else
                {
                    return new Response
                    {
                        Message = "Tài khoản không tồn tại",
                        IsSuccess = false
                    };
                }
            }
            catch
            {
                throw new NotImplementedException();
            }
        }

        public async Task<Response> UploadAvatarAsync(string userId, string avatar_url){
            try
            {
                var userCheck = await _userManager.FindByIdAsync(userId);
                if (userCheck != null)
                {
                    userCheck.Avatar = avatar_url;
                    var rs = await _userManager.UpdateAsync(userCheck);
                    if (rs.Succeeded)
                    {                
                        var account = _mapper.Map<Account, UserDto>(userCheck);
                        return new Response
                        {
                            Message = "Cập nhật thông tin tài khoản thành công",
                            Data = new Dictionary<string, object>{
                                ["user"] = account
                            },
                            IsSuccess = true
                        };

                    }
                    return new Response
                    {
                        Message = "Cập nhật tài khoản thất bại",
                        IsSuccess = false
                    };
                }
                else
                {
                    return new Response
                    {
                        Message = "Tài khoản không tồn tại",
                        IsSuccess = false
                    };
                }
            }
            catch
            {
                throw new NotImplementedException();
            }
        }
        public async Task<Response> UpdateUserAsync(string userId, UserUpdateDto user)
        {
            try
            {
                var userCheck = await _userManager.FindByIdAsync(userId);
                if (userCheck != null)
                {
                    userCheck.FullName = user.FullName;
                    if (user.Password != null)
                        userCheck.PasswordHash = _userManager.PasswordHasher.HashPassword(userCheck,user.Password);
                    var rs = await _userManager.UpdateAsync(userCheck);
                    var account = _mapper.Map<Account, UserDto>(userCheck);
                    if (rs.Succeeded)
                    {                
                        return new Response
                        {
                            Message = "Cập nhật thông tin tài khoản thành công",
                            Data = new Dictionary<string, object>{
                                ["user"] = account
                            },
                            IsSuccess = true
                        };

                    }
                    return new Response
                    {
                        Message = "Cập nhật tài khoản thất bại",
                        IsSuccess = false
                    };
                }
                else
                {
                    return new Response
                    {
                        Message = "Tài khoản không tồn tại",
                        IsSuccess = false
                    };
                }
            }
            catch
            {
                throw new NotImplementedException();
            }
        }

        public async Task<Response> GetListUsersAsync(string adminId)
        {
            var userExist = await _userManager.FindByIdAsync(adminId);
            if (userExist != null)
            {
                var userRoles = await _userManager.GetRolesAsync(userExist);
                if (userRoles[0] != "ADMIN")
                {
                    return new Response
                    {
                        Message = "Bạn không có quyền sử dụng chức năng này.",
                        IsSuccess = false
                    };
                }
                var query = @"SELECT u.Id, u.FullName, u.Email, u.EmailConfirmed, u.Avatar, r.Name as Role, w.WorkspaceQuantity
                            FROM aspnetusers u
                            INNER JOIN
                            (
                                SELECT r.Name, ur.UserId
                                FROM aspnetroles r
                                INNER JOIN aspnetuserroles ur on ur.RoleId = r.Id
                            ) as r on r.UserId = u.Id
                            LEFT JOIN
                            (
                                SELECT COUNT(w.Id) as WorkspaceQuantity, mw.UserId
                                FROM Workspaces w
                                INNER JOIN memberworkspaces mw on mw.WorkspaceId = w.Id
                                GROUP BY mw.UserId
                            ) as w on w.UserId = u.Id;";
                List<UserWorkspaceDto> userWorkspaceDtos = await _dapperContext.GetListAsync<UserWorkspaceDto>(query);

                
                return new Response
                {
                    Message = "Lấy danh sách người dùng thành công.",
                    Data = new Dictionary<string,object>{
                        ["users"]= userWorkspaceDtos
                    },
                    IsSuccess = true
                };
            }
            else
            {
                return new Response
                {
                    Message = "Tài khoản không tồn tại.",
                    IsSuccess = false
                };
            }
        }

        public async Task<Response> GetListWorkspacesByUserAsync(string adminId, string userId)
        {
            var userExist = await _userManager.FindByIdAsync(adminId);
            if (userExist != null)
            {
                var userRoles = await _userManager.GetRolesAsync(userExist);
                if (userRoles[0] != "ADMIN")
                {
                    return new Response
                    {
                        Message = "Bạn không có quyền sử dụng chức năng này.",
                        IsSuccess = false
                    };
                }
                var query = @"SELECT u.Id, u.FullName, u.Email, u.Avatar,  u.EmailConfirmed, r.Name as Role
                            FROM aspnetusers u
                            INNER JOIN
                            (
                                SELECT r.Name, ur.UserId
                                FROM aspnetroles r
                                INNER JOIN aspnetuserroles ur on ur.RoleId = r.Id
                            ) as r on r.UserId = u.Id
                            WHERE u.Id = @UserId;"+
                            @"SELECT w.Id, Title, Description, Logo, Background, Permission, CreatorId, CreatorName, TaskQuantity, TaskCompleted
                            FROM Workspaces w
                            INNER JOIN memberworkspaces mw on mw.WorkspaceId = w.Id
                            WHERE mw.UserId = @userId;";
                var parameters = new DynamicParameters();
                parameters.Add("userId", userId, DbType.String);

                UserWorkspaceDto userWorkspaceDto = null;
                using (var connection = _dapperContext.CreateConnection())
                using(var multiResult = await connection.QueryMultipleAsync(query, parameters)){
                    userWorkspaceDto = await multiResult.ReadSingleOrDefaultAsync<UserWorkspaceDto>();
                    if (userWorkspaceDto != null)
                    {
                        userWorkspaceDto.Workspaces = (await multiResult.ReadAsync<WorkspaceDto>()).ToList();
                    }

                }  
                if (userWorkspaceDto == null)
                    return new Response
                    {
                        Message = "Người dùng không tồn tại.",
                        IsSuccess = false
                    };
                return new Response
                {
                    Message = "Lấy danh sách dự án của người dùng thành công.",
                    Data = new Dictionary<string,object>{
                        ["user"]= userWorkspaceDto
                    },
                    IsSuccess = true
                };
            }
            else
            {
                return new Response
                {
                    Message = "Tài khoản không tồn tại.",
                    IsSuccess = false
                };
            }
        }

    }
}