
using Dapper;
using AutoMapper;
using TaskManager.API.Data;
using TaskManager.API.Data.DTOs;
using TaskManager.API.Data.Models;
using TaskManager.API.Services.IRepository;
using System.Data;
using Newtonsoft.Json;
using TaskManager.API.Helper;
using Microsoft.AspNetCore.SignalR;

namespace TaskManager.API.Services.Repository
{
    public class WorkspaceRepository : IWorkspaceRepository
    {
        private readonly DataContext _dataContext;
        private readonly IMapper _mapper;
        private readonly DapperContext _dapperContext;
        private readonly IWebService _webService;
        private readonly IConfiguration _configuration;
        private IHubContext<HubService, IHubService> _hubService;


        public WorkspaceRepository(DataContext dataContext, 
            IMapper mapper, 
            DapperContext dapperContext, 
            IWebService webService, 
            IConfiguration configuration, 
            IHubContext<HubService, IHubService> hubService)
        {
            _dataContext = dataContext;
            _mapper = mapper;
            _dapperContext = dapperContext;
            _webService = webService;
            _configuration = configuration;
            _hubService = hubService;
        }

        public async Task<Response> CreateWorkspaceAsync(WorkspaceDto workspaceDto, string userId, string userName)
        {
            try{
                Workspace workspace = _mapper.Map<WorkspaceDto, Workspace>(workspaceDto);
                workspace.CreateAt = DateTime.Now;
                workspace.CreatorId = userId;
                workspace.CreatorName = userName;
                workspace.Cards = new List<Card>{
                    new Card("Todos", 0),
                    new Card("In Progress", 1),
                    new Card("Completed", 2),
                };
                var wsCreated = await _dataContext.Workspaces.AddAsync(workspace);

                UserWorkspace userWorkspace = new UserWorkspace{
                                                    UserId = userId,
                                                    IsOwner = true};
                userWorkspace.Workspace = workspace;
                await _dataContext.UserWorkspaces.AddAsync(userWorkspace); 

                var activation = new Activation{
                                            UserId = userId,
                                            Content = "Create workspace",
                                            CreateAt = DateTime.Now};
                activation.Workspace = workspace;
                await _dataContext.Activations.AddAsync(activation);   

                var save = await SaveChangeAsync();
                if (save){
                    workspaceDto = _mapper.Map<Workspace, WorkspaceDto>(workspace);

                    return new Response{
                        Message = "Created workspace successfully",
                        Data = new Dictionary<string, object>{
                            ["Workspace"] = workspaceDto
                        },
                        IsSuccess = true
                    };
                }
                return new Response{
                    Message = "Created workspace is failed",
                    IsSuccess = false
                };
            }
            catch (Exception e){
                Console.WriteLine("GetWorkspaceByIdAsync: " + e.Message);
                throw e;
            }
        }

        public async Task<Response> GetWorkspaceByIdAsync(int workspaceId, string userId)
        {
            try{
                var currentDay = DateTime.Now.AddDays(-10);
                var query = @"SELECT Id, Title, Description, Logo, Background, Permission, CreatorId, CreatorName 
                              FROM Workspaces w WHERE w.Id = @WorkspaceId;" +
                            @"SELECT u.Id, u.FullName, u.Email, u.Avatar
                              FROM aspnetusers u  
                              INNER JOIN UserWorkspaces uw on u.Id = uw.UserId 
                              WHERE uw.WorkspaceId = @WorkspaceId;"+
                            @"SELECT Content, CreateAt, UserId, Avatar, FullName
                              FROM Activations a
                              INNER JOIN aspnetusers u on u.Id = a.UserId 
                              WHERE WorkspaceId = @WorkspaceId
                              ORDER BY a.Id DESC LIMIT 10;"+
                            @"SELECT Title, Content, Color, Date
                              FROM Schedules
                              WHERE WorkspaceId = @WorkspaceId AND Date >= @currentDay;";

                var parameters = new DynamicParameters();
                parameters.Add("WorkspaceId", workspaceId, DbType.Int32);  
                parameters.Add("currentDay", currentDay, DbType.DateTime);  

                WorkspaceDto workspaceDto = null;
                using (var connection = _dapperContext.CreateConnection())
                using(var multiResult = await connection.QueryMultipleAsync(query, parameters))
                {
                    workspaceDto = await multiResult.ReadSingleOrDefaultAsync<WorkspaceDto>();
                    if (workspaceDto != null){
                        workspaceDto.Members = (await multiResult.ReadAsync<UserDto>()).ToList();
                        workspaceDto.Activations = (await multiResult.ReadAsync<ActivationDto>()).ToList();
                        workspaceDto.Schedules = (await multiResult.ReadAsync<ScheduleDto>()).ToList();
                    }
                }
                if (workspaceDto == null){
                    return new Response{
                        Message = "Not found workspace",
                        IsSuccess = false
                    };
                }

                // Get list Card and Task Item
                query = @"SELECT c.Id, c.Name, c.Code, c.TaskOrder,
                                 t.Id, t.Title, t.Description, t.Priority, t.DueDate, t.CardId, t.SubtaskQuantity, t.SubtaskCompleted
                          FROM Cards c  
                          LEFT JOIN TaskItems t on c.Id = t.CardId 
                          WHERE c.WorkspaceId = @WorkspaceId;";

                parameters = new DynamicParameters();
                parameters.Add("WorkspaceId", workspaceId, DbType.Int32);  

                var cardDict = new Dictionary<int, CardDto>();
                using (var connection = _dapperContext.CreateConnection())
                {
                    var multiResult = await connection.QueryAsync<CardDto,TaskItemDto,CardDto>(
                    query, (card, taskItem)=>{
                        if(!cardDict.TryGetValue(card.Id, out var currentCard)){
                            var listTaskItem = card.TaskOrder.ConvertStringToList();
                            currentCard = card;
                            currentCard.ListTaskIdOrder = listTaskItem;

                            // create list task item = null 
                            if(listTaskItem != null)
                                for(int i = 0; i < listTaskItem.Count; i++){
                                    currentCard.TaskItems.Add(null);
                                }
                            cardDict.Add(card.Id, currentCard);
                        }
                        if (taskItem != null){
                            var index = currentCard.ListTaskIdOrder.IndexOf(taskItem.Id);
                            if (index >= 0)
                                currentCard.TaskItems[index] = taskItem;

                        }
                        return currentCard;
                    }, 
                    parameters);
                }
                workspaceDto.Cards = cardDict.Values.ToList();
                
                return new Response{
                    Message = "Get workspace successfully",
                    Data = new Dictionary<string, object>{
                        ["Workspace"] = workspaceDto
                    },
                    IsSuccess = true
                };
            }
            catch (Exception e){
                Console.WriteLine("GetWorkspaceByIdAsync: " + e.Message);
                throw e;
            }
        }

        public async Task<Response> GetWorkspacesByUserAsync(string userId)
        {
            try{
                // var workspaces =  _dataContext.Workspaces.Where( w => w.Users.FirstOrDefault(u => u.Id == userId) != null).ToList();
                var query = @"SELECT w.Id, Title, Description, Logo, Background, Permission, CreatorId, CreatorName, uw.IsOwner 
                              FROM Workspaces w
                              INNER JOIN UserWorkspaces uw on w.Id = uw.WorkspaceId
                              WHERE uw.UserId = @userId";
                var parameters = new DynamicParameters();
                parameters.Add("userId", userId, DbType.String);             
                List<WorkspaceDto> workspaceDtos = await _dapperContext.GetListAsync<WorkspaceDto>(query, parameters);
                
                // List<WorkspaceDto> workspaceDtos = _mapper.Map<List<Workspace>, List<WorkspaceDto>>(workspaces);
                
                return new Response{
                        Message = "Get workspace successfully",
                        Data = new Dictionary<string, object>{
                            ["Workspaces"] = workspaceDtos
                        },
                        IsSuccess = true
                    };
            }
            catch (Exception e){
                Console.WriteLine("GetWorkspacesByUserAsync: " + e.Message);
                throw e;
            }
        }

        public async Task<Response> DeleteWorkspaceAsync(int workspaceId)
        {
            try{
                var workspace =  _dataContext.Workspaces.FirstOrDefault(w => w.Id == workspaceId);
                if (workspace == null){
                    return new Response{
                        Message = "Not found workspace",
                        IsSuccess = false
                    };
                }
                _dataContext.Workspaces.Remove(workspace);
                var save = await SaveChangeAsync();
                if (save){
                    return new Response{
                        Message = "Deleted workspace successfully",
                        IsSuccess = true
                    };
                }
                return new Response{
                    Message = "Deleted workspace is failed",
                    IsSuccess = false
                };
            }
            catch (Exception e){
                Console.WriteLine("DeleteWorkspaceAsync: " + e.Message);
                throw e;
            }
        }  
        public async Task<Response> UpdateWorkspaceAsync(WorkspaceDto workspaceDto, string userId)
        {
            try{
                var workspace =  _dataContext.Workspaces.FirstOrDefault(w => w.Id == workspaceDto.Id);
                if (workspace == null){
                    return new Response{
                        Message = "Not found workspace",
                        IsSuccess = false
                    };
                }
                workspace.Title = workspaceDto.Title;
                workspace.Description = workspaceDto.Description;
                workspace.Permission = workspaceDto.Permission;
                workspace.UpdateAt = DateTime.Now;

                _dataContext.Workspaces.Update(workspace);

                // add activation
                var activation = new Activation{
                                            UserId = userId,
                                            Content = "Create workspace",
                                            CreateAt = DateTime.Now};
                activation.Workspace = workspace;
                await _dataContext.Activations.AddAsync(activation);  

                var save = await SaveChangeAsync();
                if (save){
                    // Send workspace to clients
                    workspaceDto = _mapper.Map<Workspace, WorkspaceDto>(workspace);         
    	            await _hubService.Clients.Group($"Workspace-{workspaceDto.Id}").SendWorkspaceAsync(workspaceDto);

                    return new Response{
                        Message = "Updated workspace successfully",
                        IsSuccess = true
                    };
                }
                return new Response{
                    Message = "Updated workspace is failed",
                    IsSuccess = false
                };
            }
            catch (Exception e){
                Console.WriteLine("UpdateWorkspaceAsync: " + e.Message);
                throw e;
            }
        }
        public async Task<bool> SaveChangeAsync()
        {
            return await _dataContext.SaveChangesAsync()>0;
        }

        public async Task<Response> InviteUserToWorkspaceAsync(int workspaceId, string email)
        {
            try{
                var query = @"SELECT Id, Email, FullName FROM aspnetusers WHERE Email= @email AND EmailConfirmed = 1";
                var user = await _dapperContext.GetFirstAsync<UserDto>(query, new { email });
                if (user != null){
                    var url = $"{_configuration["RootUrl"]}api/Workspace/invite/confirmed?workspaceId={workspaceId}&userId={user.Id}";

                    #region html content send email confirmation
                    var body = "<div style=\"width:100%; height:100vh; background-color: #d0e7fb; display: flex; align-items: center; justify-content: center; margin:auto; box-sizing: border-box;\" >"
                            + "<div style =\"font-family: 'Lobster', cursive; margin:auto; border-radius: 4px;padding: 40px;min-width: 200px;max-width: 40%; background-color: azure;\" >"
                            + "<p style=\"margin: 0; padding: 10px 0 ;font-size: 2rem;width: 100%;text-align:left\">Invite workspace</p>"
                            + "<p style=\"margin: 0;width:100%;padding: 10px 0;text-align: left;color: #7e7b7b;\">Please accept workspace by clicking the link below so you can access to <span style=\"color:#447eb0\" >Task Tracking</span> system account.</p>"
                            + $"<a href=\"{url}\" style=\" padding: 10px 20px; background-color: #439b73;color: #ffff;text-decoration:none;border-radius: 3px;font-weight:500;display:block; text-align:center; margin-top:10px;margin-bottom:10px;\" >Accept to workspace</a>"
                            + "</div> </div>";
                    #endregion
                    // Send email to the user
                    var content = new EmailOption{
                        ToEmail = email,
                        Subject = "Accept to workspace",
                        Body = body
                    };

                    var send = await _webService.SendEmail(content);
                    return new Response{
                        Message = "Send email to the user",
                        IsSuccess = true
                    };
                }
                return new Response{
                    Message = "Not Found user",
                    IsSuccess = false
                };
            }
            catch (Exception e){
                Console.WriteLine("InviteUserToWorkspaceAsync: " + e.Message);
                throw e;
            }
        }

        public async Task<Response> ConfirmMemberWorkspaceAsync(int workspaceId, string userId)
        {
            try{
                var query = @"SELECT Id FROM Workspaces WHERE Id=@workspaceId";
                var workspace = _dapperContext.GetFirstAsync<Workspace>(query, new { workspaceId });
                if (workspace != null){
                    var us = new UserWorkspace{
                        WorkspaceId = workspaceId,
                        IsOwner = false,
                        UserId = userId
                    };
                    _dataContext.UserWorkspaces.Add(us);

                    // add activation
                    var activation = new Activation{
                                                UserId = userId,
                                                WorkspaceId = workspaceId,
                                                Content = "Create workspace",
                                                CreateAt = DateTime.Now};
                    await _dataContext.Activations.AddAsync(activation); 

                    var isSave = await SaveChangeAsync();
                    if(isSave){
                        var activationDto = _mapper.Map<Activation, ActivationDto>(activation);
    	                await _hubService.Clients.Group($"Workspace-{workspaceId}").SendActivationAsync(activationDto);
                        
                        return new Response{
                            Message = $"{_configuration["RootUrl"]}ConfirmEmail.html",
                            IsSuccess = true
                        };
                    }
                    return new Response{
                        Message = $"Not confirmed",
                        IsSuccess = false
                    };
                }
                return new Response{
                    Message = "Not Found user",
                    IsSuccess = false
                };
            }
            catch (Exception e){
                Console.WriteLine("InviteUserToWorkspaceAsync: " + e.Message);
                throw e;
            }
        }
    }
}