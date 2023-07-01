
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
        private readonly GetData _getData;


        public WorkspaceRepository(DataContext dataContext,
            IMapper mapper,
            DapperContext dapperContext,
            IWebService webService,
            IConfiguration configuration,
            IHubContext<HubService, IHubService> hubService,
            GetData getData = null)
        {
            _dataContext = dataContext;
            _mapper = mapper;
            _dapperContext = dapperContext;
            _webService = webService;
            _configuration = configuration;
            _hubService = hubService;
            _getData = getData;
        }

        public async Task<Response> CreateWorkspaceAsync(WorkspaceDto workspaceDto, string userId, string userName)
        {
            try{
                Workspace workspace = _mapper.Map<WorkspaceDto, Workspace>(workspaceDto);
                workspace.CreateAt = DateTime.Now;
                workspace.CreatorId = userId;
                workspace.CreatorName = userName;
                workspace.Cards = new List<Card>{
                    new Card("Sẽ làm", CARD_CODE_ENUM.Todos),
                    new Card("Đang làm", CARD_CODE_ENUM.InProgress),
                    new Card("Hoàn thành", CARD_CODE_ENUM.Completed),
                };
                var wsCreated = await _dataContext.Workspaces.AddAsync(workspace);

                MemberWorkspace userWorkspace = new MemberWorkspace{
                                                    UserId = userId,
                                                    Role = 0};
                userWorkspace.Workspace = workspace;
                await _dataContext.MemberWorkspaces.AddAsync(userWorkspace); 

                var activation = new Activation{
                                            UserId = userId,
                                            Content = "Tạo dự án",
                                            CreateAt = DateTime.Now};
                activation.Workspace = workspace;
                await _dataContext.Activations.AddAsync(activation);   

                var save = await SaveChangeAsync();
                if (save){
                    workspaceDto = _mapper.Map<Workspace, WorkspaceDto>(workspace);

                    return new Response{
                        Message = "Tạo dự án thành công",
                        Data = new Dictionary<string, object>{
                            ["Workspace"] = workspaceDto
                        },
                        IsSuccess = true
                    };
                }
                return new Response{
                    Message = "Tạo dự án thất bại",
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
                var mbWorkspace = _dataContext.MemberWorkspaces.FirstOrDefault(w => w.WorkspaceId == workspaceId && w.UserId == userId);
                if (mbWorkspace == null)
                    return new Response{
                        Message = "Bạn không phải là thành viên dự án.",
                        IsSuccess = false
                    };
                mbWorkspace.VisitDate = DateTime.Now;
                _dataContext.MemberWorkspaces.Update(mbWorkspace);
                await SaveChangeAsync();

                var currentDay = DateTime.Now.AddDays(-10);
                var query = @"SELECT Id, Title, Description, Permission, CreatorId, CreatorName, TaskQuantity, TaskCompleted 
                              FROM Workspaces w WHERE w.Id = @WorkspaceId;" +
                            @"SELECT mw.UserId, u.FullName, u.Email, u.Avatar, mw.Role
                              FROM aspnetusers u
                              INNER JOIN MemberWorkspaces mw on u.Id = mw.UserId 
                              WHERE mw.WorkspaceId = @WorkspaceId;"+
                            @"SELECT Id, Name, Code, TaskQuantity, TaskOrder
                              FROM Cards
                              WHERE WorkspaceId = @WorkspaceId;"+
                            @"SELECT Content, CreateAt, UserId, Avatar, FullName
                              FROM Activations a
                              INNER JOIN aspnetusers u on u.Id = a.UserId 
                              WHERE WorkspaceId = @WorkspaceId
                              ORDER BY a.CreateAt DESC LIMIT 10;";
                            // @"SELECT s.*, u.FullName, u.Email, u.Avatar
                            //   FROM Schedules s
                            //   INNER JOIN aspnetusers u on u.Id = s.CreatorId
                            //   WHERE s.WorkspaceId = @workspaceId;";

                var parameters = new DynamicParameters();
                parameters.Add("WorkspaceId", workspaceId, DbType.Int32);  
                parameters.Add("currentDay", currentDay, DbType.DateTime);  

                WorkspaceDto workspaceDto = null;
                using (var connection = _dapperContext.CreateConnection())
                using(var multiResult = await connection.QueryMultipleAsync(query, parameters))
                {
                    workspaceDto = await multiResult.ReadSingleOrDefaultAsync<WorkspaceDto>();
                    if (workspaceDto != null){
                        workspaceDto.Members = (await multiResult.ReadAsync<MemberWorkspaceDto>()).ToList();
                        if(workspaceDto.Members != null){

                            // Check member has been workspace
                            var isMember = false;
                            foreach (var member in workspaceDto.Members){
                                if (member.UserId == userId)
                                {
                                    isMember = true;
                                    workspaceDto.MyRole =  (int)member.Role;
                                }
                            }

                            if(!isMember && workspaceDto.Permission == 1)
                                return new Response{
                                    Message = "Bạn không có quyền xem dự án này.",
                                    IsSuccess = false
                                };

                            // // Move user to the first of list
                            // var index = workspaceDto.Members.FindIndex(x => x.Id == userId);
                            // if (index > 0){
                            //     var temp = workspaceDto.Members[0];
                            //     workspaceDto.Members[0] = workspaceDto.Members[index];
                            //     workspaceDto.Members[index] = temp;
                            // }
                        }

                        workspaceDto.Cards = (await multiResult.ReadAsync<CardDto>()).ToList();
                        foreach (var card in workspaceDto.Cards)
                        {
                            var listTaskItem = card.TaskOrder.ConvertStringToList();
                            card.ListTaskIdOrder = listTaskItem; 
                            if(listTaskItem != null)
                                for(int i = 0; i < listTaskItem.Count; i++){
                                    card.TaskItems.Add(null);
                                }
                        }
                        workspaceDto.Activations = (await multiResult.ReadAsync<ActivationDto>()).ToList();
                        // workspaceDto.Schedules = (await multiResult.ReadAsync<ScheduleDto>()).ToList();
                    }
                }
                if (workspaceDto == null){
                    return new Response{
                        Message = "Không tồn tại dự án.",
                        IsSuccess = false
                    };
                }

                // Get list member and Task Item
                query = @"SELECT t.Id, t.Title, t.Description, t.Priority, t.DueDate, t.CardId, t.IsComplete, t.SubtaskQuantity, t.SubtaskCompleted, t.CommentQuantity
                                ,m.UserId, m.FullName, m.Avatar, m.Email, m.TaskItemId
		                        ,l.Id, l.Name, l.Color, l.WorkspaceId
                          FROM 
                          (
                            SELECT t.Id, t.Title, t.Description, t.Priority, t.DueDate, t.CardId, t.IsComplete, t.SubtaskQuantity, t.SubtaskCompleted, t.CommentQuantity, c.WorkspaceId
                            FROM TaskItems t  
                            INNER JOIN Cards c on c.Id = t.CardId 
                          ) as t 
                          LEFT JOIN 
                          (
                            SELECT u.Id as UserId, u.FullName, u.Avatar, u.Email, mt.TaskItemId
                            FROM aspnetusers u  
                            INNER JOIN MemberTasks mt on mt.UserId = u.Id  
                          ) as m on m.TaskItemId = t.Id
                          LEFT JOIN 
                          (
                            SELECT l.Id, Name, Color, WorkspaceId, tl.TaskItemId
                            FROM Labels l
                            INNER JOIN TaskLabels tl on tl.LabelId = l.Id 
                          ) as l on l.TaskItemId = t.id

                          WHERE t.WorkspaceId = @WorkspaceId;";

                parameters = new DynamicParameters();
                parameters.Add("WorkspaceId", workspaceId, DbType.Int32);  

                var taskItemDict = new Dictionary<int, TaskItemDto>();
                using (var connection = _dapperContext.CreateConnection())
                {
                    var multiResult = await connection.QueryAsync<TaskItemDto,MemberTaskDto, LabelDto,TaskItemDto>(
                    query, (taskItem, memberTask, label)=>{
                        if(!taskItemDict.TryGetValue(taskItem.Id, out var currenttaskItem)){
                            currenttaskItem = taskItem;
                            taskItemDict.Add(taskItem.Id, currenttaskItem);
                        }
                        if (memberTask != null && currenttaskItem.Members.FirstOrDefault(m => m.UserId == memberTask.UserId) == null){
                            currenttaskItem.Members.Add(memberTask);
                        }
                        if (label != null 
                            && currenttaskItem.Labels.FirstOrDefault(m => m.Id == label.Id) == null){
                            currenttaskItem.Labels.Add(label);
                        }
                        return currenttaskItem;
                    }, 
                    parameters
                    , splitOn: "UserId, Id");
                }

                // Add task item to card
                foreach (var taskItem in taskItemDict.Values){
                    foreach (var card in workspaceDto.Cards){
                        if(card.TaskQuantity > 0)
                        {
                            var index = card.ListTaskIdOrder.IndexOf(taskItem.Id);
                            if (index >= 0)
                            {
                                card.TaskItems[index] = taskItem;
                                break;
                            }
                        }
                    }
                }


                
                return new Response{
                    Message = "Lấy dự án thành công",
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
        public async Task<Response> GetWorkspaceRecentlyAsync(string userId)
        {
            try{

                var query = @"SELECT W.*, mw.*
                            FROM
                            (
                                SELECT w.Id, Title, Description, Permission, CreatorId, CreatorName, TaskQuantity, TaskCompleted, mw.Role as MyRole, mw.VisitDate
                                FROM Workspaces w
                                INNER JOIN MemberWorkspaces mw on w.Id = mw.WorkspaceId
                                WHERE mw.UserId = @userId
                            ) as w
                            LEFT JOIN 
                            (
                                SELECT mw.UserId, FullName, Email, avatar, mw.WorkspaceId
                                FROM aspnetusers u
                                INNER JOIN MemberWorkspaces mw on mw.UserId = u.Id
                            ) as mw ON mw.WorkspaceId = w.Id
                            ORDER BY w.VisitDate DESC
                            LIMIT 5";
                var parameters = new DynamicParameters();
                parameters.Add("userId", userId, DbType.String);   
                // parameters.Add("userId", "0d5a3063-5c35-4c7f-896f-4048420d2c17", DbType.String);  

                var workspaceDict = new Dictionary<int, WorkspaceDto>();
                using (var connection = _dapperContext.CreateConnection())
                {
                    await connection.QueryAsync<WorkspaceDto,MemberWorkspaceDto, WorkspaceDto>(
                    query, (workspace, memberWorkspace)=>{
                        if(!workspaceDict.TryGetValue(workspace.Id, out var currentWorkspace)){
                            currentWorkspace = workspace;
                            workspaceDict.Add(workspace.Id, currentWorkspace);
                        }
                        if (memberWorkspace != null ){
                            currentWorkspace.Members.Add(memberWorkspace);
                        }
                        return currentWorkspace;
                    }, 
                    parameters
                    , splitOn: "UserId");
                }
                var workspaceDtos = workspaceDict.Values.ToList();

                if (workspaceDtos == null)
                    workspaceDtos = new List<WorkspaceDto>();
                return new Response{
                        Message = "Lấy danh sách dự án thành công",
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
        public async Task<Response> GetWorkspacesByUserAsync(string userId)
        {
            try{
                var query = @"SELECT W.*, mw.*
                            FROM
                            (
                                SELECT w.Id, Title, Description, mw.VisitDate, Permission, CreatorId, CreatorName, TaskQuantity, TaskCompleted, mw.Role as MyRole
                                FROM Workspaces w
                                INNER JOIN MemberWorkspaces mw on w.Id = mw.WorkspaceId
                                WHERE mw.UserId = @userId
                            ) as w
                            LEFT JOIN 
                            (
                                SELECT mw.UserId, FullName, Email, avatar, mw.WorkspaceId
                                FROM aspnetusers u
                                INNER JOIN MemberWorkspaces mw on mw.UserId = u.Id
                            ) as mw ON mw.WorkspaceId = w.Id
                            ORDER BY w.VisitDate DESC";
                var parameters = new DynamicParameters();
                parameters.Add("userId", userId, DbType.String);  
                // parameters.Add("userId", "0d5a3063-5c35-4c7f-896f-4048420d2c17", DbType.String);  

                var workspaceDict = new Dictionary<int, WorkspaceDto>();
                using (var connection = _dapperContext.CreateConnection())
                {
                    await connection.QueryAsync<WorkspaceDto,MemberWorkspaceDto, WorkspaceDto>(
                    query, (workspace, memberWorkspace)=>{
                        if(!workspaceDict.TryGetValue(workspace.Id, out var currentWorkspace)){
                            currentWorkspace = workspace;
                            workspaceDict.Add(workspace.Id, currentWorkspace);
                        }
                        if (memberWorkspace != null ){
                            currentWorkspace.Members.Add(memberWorkspace);
                        }
                        return currentWorkspace;
                    }, 
                    parameters
                    , splitOn: "UserId");
                }
                var workspaceDtos = workspaceDict.Values.ToList();

                if (workspaceDtos == null)
                    workspaceDtos = new List<WorkspaceDto>();
                return new Response{
                        Message = "Lấy danh sách dự án thành công",
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

        public async Task<Response> DeleteWorkspaceAsync(int workspaceId, string userId)
        {
            try{
                var permission = _dataContext.MemberWorkspaces.FirstOrDefault(x => x.WorkspaceId == workspaceId && x.UserId == userId);
                if (permission.Role != ROLE_ENUM.Owner)
                {
                        return new Response
                        {
                            Message = "Bạn không có quyền xóa dự án này.",
                            IsSuccess = false
                        };
                }
                var workspace =  _dataContext.Workspaces.FirstOrDefault(w => w.Id == workspaceId);
                if (workspace == null){
                    return new Response{
                        Message = "dự án không tồn tại",
                        IsSuccess = false
                    };
                }
                _dataContext.Workspaces.Remove(workspace);
                
                var query =@"SELECT mt.Id, mt.UserId, mt.TaskItemId
                            FROM TaskItems t
                            INNER JOIN Cards c on c.id = t.CardId
                            INNER JOIN MemberTasks mt on mt.TaskItemId = t.Id
                            WHERE c.WorkspaceId = @workspaceId";
                var parameters = new DynamicParameters();
                parameters.Add("WorkspaceId", workspaceId, DbType.Int32); 

                List<MemberTask> membersTask = await _dapperContext.GetListAsync<MemberTask>(query, parameters);
                _dataContext.MemberTasks.RemoveRange(membersTask);
                
                var save = await SaveChangeAsync();
                if (save){
                    return new Response{
                        Message = "Xóa dự án thành công",
                        IsSuccess = true
                    };
                }
                return new Response{
                    Message = "Không thể xóa dự án",
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
                        Message = "Không tìm thấy dự án",
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
    	            // await _hubService.Clients.Group($"Workspace-{workspaceDto.Id}").WorkspaceAsync(workspaceDto);
                    // Send SignalR
                    var resWorkspaceDto = await _getData.GetWorkspaceById(workspaceDto.Id, userId);
                    await _hubService.Clients.Group($"workspace-{workspaceDto.Id}").WorkspaceAsync(resWorkspaceDto);
                    return new Response{
                        Message = "Cập nhật dự án thành công.",
                        IsSuccess = true
                    };
                }
                return new Response{
                    Message = "Cập nhật dự án thất bại.",
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

        public async Task<Response> GetCardsOfWorkspaceAsync(int workspaceId){
            try{
                var cards = _dataContext.Cards.Where(c => c.WorkspaceId == workspaceId).ToList();
                if (cards == null) 
                    return new Response{
                        Message = "Dự án không tồn tại",
                        IsSuccess = false
                    }; 
                cards.RemoveAt(2);
                return new Response{
                    Message = "Lấy danh sách thẻ thành công",
                    Data = new Dictionary<string, object>{
                        ["Card"] = cards
                    },
                    IsSuccess = true
                };

            }
            catch(Exception e){
                Console.WriteLine("GetCardsOfWorkspaceAsync: " + e.Message);
                throw e;
            }
        }

        #region Member in workspace
        public async Task<Response> InviteMemberToWorkspaceAsync(int workspaceId, string userId, MemberWorkspaceDto member)
        {
            try{
                var uwAdmin = _dataContext.MemberWorkspaces.FirstOrDefault(x => x.WorkspaceId == workspaceId && x.UserId == userId);
                if (uwAdmin.Role != ROLE_ENUM.Owner){
                    return new Response{
                        Message = $"Bạn không có quyền mời thành viên",
                        IsSuccess = false
                    };
                }

                var user = _dataContext.Users.FirstOrDefault(u => u.Email == member.Email && u.EmailConfirmed==true);
                if (user != null){
                    MemberWorkspace memberExist = _dataContext.MemberWorkspaces.FirstOrDefault(m => m.WorkspaceId == workspaceId && m.UserId == user.Id);
                    if (memberExist != null)
                        return new Response{
                            Message = "Thành viên đã tham gia dự án",
                            IsSuccess = true
                        };
                    Console.WriteLine(member.Role);
                    var url = $"{_configuration["RootUrl"]}api/Workspace/Invite/Confirmed?workspaceId={workspaceId}&userId={user.Id}&role={(int)member.Role}";

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
                        ToEmail = member.Email,
                        Subject = "Accept to workspace",
                        Body = body
                    };

                    var send = await _webService.SendEmail(content);
                    return new Response{
                        Message = "Đã gửi yêu cầu tới thành viên",
                        IsSuccess = true
                    };
                }
                return new Response{
                    Message = "Không tìm thấy thành viên",
                    IsSuccess = false
                };
            }
            catch (Exception e){
                Console.WriteLine("InviteUserToWorkspaceAsync: " + e.Message);
                throw e;
            }
        }
        public async Task<Response> ConfirmMemberWorkspaceAsync(int workspaceId, string userId, int role)
        {
            try{
                var query = @"SELECT Id FROM Workspaces WHERE Id=@workspaceId";
                var workspace = _dapperContext.GetFirstAsync<Workspace>(query, new { workspaceId });
                if (workspace != null){
                    var us = new MemberWorkspace{
                        WorkspaceId = workspaceId,
                        Role = (ROLE_ENUM) role,
                        UserId = userId
                    };
                    _dataContext.MemberWorkspaces.Add(us);

                    // add activation
                    var activation = new Activation{
                                                UserId = userId,
                                                WorkspaceId = workspaceId,
                                                Content = "Tham gia dự án",
                                                CreateAt = DateTime.Now};
                    await _dataContext.Activations.AddAsync(activation); 

                    var isSave = await SaveChangeAsync();
                    if(isSave){
                        // var activationDto = _mapper.Map<Activation, ActivationDto>(activation);
    	                // await _hubService.Clients.Group($"Workspace-{workspaceId}").ActivationAsync(activationDto);
                        // Send SignalR
                        var responseWorkspaceDto = await _getData.GetWorkspaceById(workspaceId, userId);
                        await _hubService.Clients.Group($"workspace-{workspaceId}").WorkspaceAsync(responseWorkspaceDto);
                        return new Response{
                            Message = $"{_configuration["RootUrl"]}ConfirmEmail.html",
                            IsSuccess = true
                        };
                    }
                    return new Response{
                        Message = $"{_configuration["RootUrl"]}ConfirmEmailError.html",
                        IsSuccess = false
                    };
                }
                return new Response{
                    Message = $"{_configuration["RootUrl"]}ConfirmEmailError.html",
                    IsSuccess = false
                };
            }
            catch (Exception e){
                Console.WriteLine("ConfirmMemberWorkspaceAsync: " + e.Message);
                throw e;
            }
        }
        
        public async Task<Response> RemoveMemberToWorkspaceAsync(int workspaceId, string userId, string memberId)
        {
            try{
                var uwAdmin = _dataContext.MemberWorkspaces.FirstOrDefault(x => x.WorkspaceId == workspaceId && x.UserId == userId);
                if (uwAdmin != null){
                    if (uwAdmin.Role == ROLE_ENUM.Owner){
                        var uwMember = _dataContext.MemberWorkspaces.FirstOrDefault(x => x.WorkspaceId == workspaceId && x.UserId == memberId);
                        // if (uwMember.Role == ROLE_ENUM.Admin && uwAdmin.Role == ROLE_ENUM.Admin)
                        //     return new Response{
                        //         Message = $"Bạn không có quyền xóa thành viên",
                        //         IsSuccess = false
                        //     };
                        
                        _dataContext.MemberWorkspaces.Remove(uwMember);

                        // Remove member tasks
                        var query =@"SELECT mt.Id, mt.UserId, mt.TaskItemId
                            FROM TaskItems t
                            INNER JOIN Cards c on c.id = t.CardId
                            INNER JOIN MemberTasks mt on mt.TaskItemId = t.Id
                            WHERE mt.UserId = @userId AND c.WorkspaceId = @workspaceId";
                        var parameters = new DynamicParameters();
                        parameters.Add("userId", userId, DbType.String); 
                        parameters.Add("WorkspaceId", workspaceId, DbType.Int32); 

                        List<MemberTask> membersTask = await _dapperContext.GetListAsync<MemberTask>(query, parameters);
                        _dataContext.MemberTasks.RemoveRange(membersTask);

                        // add activation
                        var activation = new Activation{
                                                    UserId = userId,
                                                    WorkspaceId = workspaceId,
                                                    Content = "đã xóa thành viên khỏi dự án",
                                                    CreateAt = DateTime.Now};
                        await _dataContext.Activations.AddAsync(activation); 

                        var isSave = await SaveChangeAsync();
                        if(isSave){
                            
                            // Send SignalR
                            var responseWorkspaceDto = await _getData.GetWorkspaceById(workspaceId, userId);
                            await _hubService.Clients.Group($"workspace-{workspaceId}").WorkspaceAsync(responseWorkspaceDto);
                            return new Response{
                                Message = "Đã xóa thành viên khỏi dự án",
                                IsSuccess = true
                            };
                        }
                        return new Response{
                            Message = $"Không thể xóa thành viên khỏi dự án",
                            IsSuccess = false
                        };
                    }

                    return new Response{
                        Message = $"Bạn không có quyền xóa thành viên",
                        IsSuccess = false
                    };
                }
                return new Response{
                    Message = "Bạn không phải là thành viên dự án",
                    IsSuccess = false
                };
            }
            catch (Exception e){
                Console.WriteLine("InviteUserToWorkspaceAsync: " + e.Message);
                throw e;
            }
        }
        
        public async Task<Response> GetMembersOfWorkspaceAsync(int workspaceId)
        {
            try
            {
                var query = @"SELECT uw.UserId, u.FullName, u.Avatar, u.Email, uw.Role
                              FROM aspnetusers u 
                              INNER JOIN MemberWorkspaces uw on uw.UserId = u.Id
                              WHERE uw.WorkspaceId = @workspaceId";

                var parameters = new DynamicParameters();
                parameters.Add("workspaceId", workspaceId, DbType.Int32);  

                List<MemberWorkspaceDto> members = await _dapperContext.GetListAsync<MemberWorkspaceDto>(query, parameters);

                return new Response{
                    Message = "Lấy danh sách thành viên thành công",
                    Data = new Dictionary<string, object>{
                        ["Members"] = members
                    },
                    IsSuccess = true
                };
            }
            catch (Exception e)
            {
                Console.WriteLine("GetMemberOfWorkspaceAsync: " + e.Message);
                throw e;
            }
        }
        public async Task<Response> GetMembersWithTaskItemAsync(int workspaceId, string userId)
        {
            try
            {
                var query = @"SELECT mw.UserId, u.FullName, u.Avatar, u.Email, mw.Role
                              FROM aspnetusers u 
                              INNER JOIN MemberWorkspaces mw on mw.UserId = u.Id
                              WHERE mw.WorkspaceId = @workspaceId";

                var parameters = new DynamicParameters();
                parameters.Add("workspaceId", workspaceId, DbType.Int32);  
                List<MemberWorkspaceDto> membersWorkspace = await _dapperContext.GetListAsync<MemberWorkspaceDto>(query, parameters);
                var n = membersWorkspace.Count;
                if(n >0){
                    string Ids = "";
                    for(int i=0; i< n; i++){
                        Ids += $"'{membersWorkspace[i].UserId}'";
                        if (i < n - 1)
                            Ids += ",";
                    }

                    query = $@"SELECT mt.UserId, COUNT(IsComplete) as TaskQuantity, SUM(IsComplete) as CompletedQuantity
                               FROM MemberTasks mt
                               LEFT JOIN 
                               (
                                SELECT t.Id, t.IsComplete, c.WorkspaceId
                                FROM TaskItems t
                                INNER JOIN Cards c on c.Id = t.CardId
                                ) as t on t.Id = mt.TaskItemId
                               WHERE mt.UserId in ({Ids}) AND t.WorkspaceId = {workspaceId}
                               GROUP BY mt.UserId";

                    List<MemberWorkspaceDto> membersWithTask = await _dapperContext.GetListAsync<MemberWorkspaceDto>(query);
                    
                    for (int i = 0; i < membersWorkspace.Count; i++)
                    {
                        var memberTask = membersWithTask.FirstOrDefault(
                            item => item.UserId == membersWorkspace[i].UserId
                        );
                        if (memberTask != null){
                            membersWorkspace[i].CompletedQuantity = memberTask.CompletedQuantity;
                            membersWorkspace[i].TaskQuantity = memberTask.TaskQuantity;
                        }

                        // Move user to the first of list
                        if (i>0 && membersWorkspace[i].UserId == userId)
                        {
                            var temp = membersWorkspace[0];
                            membersWorkspace[0] = membersWorkspace[i];
                            membersWorkspace[i] = temp;
                            continue;
                        }                       
                    }

                    // // Move user to the first of list
                    // var index = membersWorkspace.FindIndex(x => x.Id == userId);
                    // var temp = membersWorkspace[0];
                    // membersWorkspace[0] = membersWorkspace[index];
                    // membersWorkspace[index] = temp;
                    
                    return new Response{
                        Message = "Lấy danh sách thành viên thành công",
                        Data = new Dictionary<string, object>{
                            ["Members"] = membersWorkspace
                        },
                        IsSuccess = true
                    };
                    
                }
                return new Response{
                    Message = "Không tìm thấy thành viên",
                    IsSuccess = false
                };
            }
            catch (Exception e)
            {
                Console.WriteLine("GetMembersWithTaskItemAsync: " + e.Message);
                throw e;
            }
        }
        public async Task<Response> LeaveOnWorkspaceAsync(int workspaceId, string userId)
        {
            try{
                var userWorkspace = _dataContext.MemberWorkspaces.FirstOrDefault(x => x.WorkspaceId == workspaceId && x.UserId == userId);
                if (userWorkspace != null){
                    if(userWorkspace.Role == ROLE_ENUM.Owner){
                        return new Response{
                        Message = $"Bạn là người tạo dự án. Không thể rời dự án",
                        IsSuccess = false
                    };
                    }
                    _dataContext.MemberWorkspaces.Remove(userWorkspace);

                    // Remove member tasks
                    var query =@"SELECT mt.Id, mt.UserId, mt.TaskItemId
                        FROM TaskItems t
                        INNER JOIN Cards c on c.id = t.CardId
                        INNER JOIN MemberTasks mt on mt.TaskItemId = t.Id
                        WHERE mt.UserId = @userId AND c.WorkspaceId = @workspaceId";
                    var parameters = new DynamicParameters();
                    parameters.Add("userId", userId, DbType.String); 
                    parameters.Add("WorkspaceId", workspaceId, DbType.Int32); 

                    List<MemberTask> membersTask = await _dapperContext.GetListAsync<MemberTask>(query, parameters);
                    _dataContext.MemberTasks.RemoveRange(membersTask);

                    // add activation
                    var activation = new Activation{
                                                UserId = userId,
                                                WorkspaceId = workspaceId,
                                                Content = "rời khỏi dự án",
                                                CreateAt = DateTime.Now};
                    await _dataContext.Activations.AddAsync(activation); 

                    var isSave = await SaveChangeAsync();
                    if(isSave){
                       
                        // Send SignalR
                        var responseWorkspaceDto = await _getData.GetWorkspaceById(workspaceId, userId);
                        await _hubService.Clients.Group($"workspace-{workspaceId}").WorkspaceAsync(responseWorkspaceDto);
                        return new Response{
                            Message = "Rời khỏi dự án thành công",
                            IsSuccess = true
                        };
                    }
                    return new Response{
                        Message = $"Không thể rời dự án",
                        IsSuccess = false
                    };
                }
                return new Response{
                    Message = "Bạn không phải là thành viên dự án",
                    IsSuccess = false
                };
            }
            catch (Exception e){
                Console.WriteLine("InviteUserToWorkspaceAsync: " + e.Message);
                throw e;
            }
        }
        #endregion


    }
}