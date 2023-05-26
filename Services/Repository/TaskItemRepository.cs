using System.Data;
using AutoMapper;
using Dapper;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.SignalR;
using Newtonsoft.Json;
using TaskManager.API.Data;
using TaskManager.API.Data.DTOs;
using TaskManager.API.Data.Models;
using TaskManager.API.Services.IRepository;

namespace TaskManager.API.Services.Repository
{
    public class TaskItemRepository : ITaskItemRepository
    {
        private readonly DataContext _dataContext;
        private readonly IMapper _mapper;
        private readonly DapperContext _dapperContext;
        private readonly IWebService _webService;
        private IHubContext<HubService, IHubService> _hubService;

        public TaskItemRepository(DataContext dataContext, IMapper mapper, DapperContext dapperContext, IWebService webService, IHubContext<HubService, IHubService> hubService)
        {
            _dataContext = dataContext;
            _mapper = mapper;
            _dapperContext = dapperContext;
            _webService = webService;
            _hubService = hubService;
        }

        public async Task<Response> CreateTaskItemAsync(int workspaceId, string userId, TaskItemDto taskItemDto)
        {
            try{
                TaskItem taskItem = _mapper.Map<TaskItemDto, TaskItem>(taskItemDto);
                taskItem.CreatorId = userId;
                var a = await _dataContext.TaskItems.AddAsync(taskItem);
            
                var isSaved = await SaveChangeAsync();
                if (isSaved){
                    // Update tasks order of card
                    var card = _dataContext.Cards.FirstOrDefault(c => c.Id == taskItemDto.CardId);
                    List<int> listTaskItem = null;

                    if(card.TaskOrder != ""){
                        listTaskItem = JsonConvert.DeserializeObject<List<int>>(card.TaskOrder);
                    }
                    else
                        listTaskItem = new List<int>();

                    listTaskItem.Add(taskItem.Id);
                    card.TaskOrder = JsonConvert.SerializeObject(listTaskItem);
                    card.TaskQuantity += 1;
                    _dataContext.Cards.Update(card);
                    
                    var activation = new Activation{
                        UserId = userId,
                        WorkspaceId = workspaceId,
                        Content = $"Create task {taskItem.Title} in card {card.Name}",
                        CreateAt = DateTime.Now
                    };
                    await _dataContext.Activations.AddAsync(activation);

                    isSaved = await SaveChangeAsync();

                    // Send changed card to client hub
                    var cardDto = _mapper.Map<Card, CardDto>(card);
                    var activationDto = _mapper.Map<Activation, ActivationDto>(activation);
    	            await _hubService.Clients.Group($"Workspace-{workspaceId}").SendCardAsync(cardDto);
    	            await _hubService.Clients.Group($"Workspace-{workspaceId}").SendActivationAsync(activationDto);

                    taskItemDto = _mapper.Map<TaskItem, TaskItemDto>(taskItem);
                    return new Response{
                        Message = "Created task item is successed",
                        Data = new Dictionary<string, object>{
                            ["TaskItem"] = taskItemDto,
                        },
                        IsSuccess = false
                    };
                }
                return new Response{
                    Message = "Created task item is is failed",
                    IsSuccess = false
                };
            }
            catch(Exception e){
                Console.WriteLine("CreateItemAsync: " + e.Message);
                throw e;
            }
        }

        public async Task<Response> GetTaskItemByIdAsync(int taskItemId)
        {
            try
            {
                var query = @"SELECT t.Id, Title, Description, Attachment, Priority, DueDate, CardId, IsComplete, SubtaskQuantity, SubtaskCompleted,
                                     u.Id as CreatorId, u.FullName, u.Avatar, u.Email
                              FROM TaskItems t 
                              INNER JOIN aspnetusers u on u.Id = t.CreatorId
                              WHERE t.Id = @taskItemId;" +
                            @"SELECT mt.Id, u.Id as UserId, u.FullName, u.Avatar, u.Email, mt.TaskItemId
                              FROM aspnetusers u  
                              INNER JOIN MemberTasks mt on u.Id = mt.UserId 
                              WHERE mt.TaskItemId = @taskItemId;"+
                            @"SELECT c.Id, c.Content, c.UpdateAt, u.FullName, u.Avatar, c.UserId, c.TaskItemId
                              FROM aspnetusers u  
                              INNER JOIN Comments c on u.Id = c.UserId
                              WHERE c.TaskItemId = @taskItemId;"+
                            @"SELECT ID, Name, Status
                              FROM Subtasks  
                              WHERE TaskItemId = @taskItemId;";

                var parameters = new DynamicParameters();
                parameters.Add("taskItemId", taskItemId, DbType.Int32);  

                TaskItemDto taskItemDto = null;
                using (var connection = _dapperContext.CreateConnection())
                using(var multiResult = await connection.QueryMultipleAsync(query, parameters))
                {
                    taskItemDto = await multiResult.ReadSingleOrDefaultAsync<TaskItemDto>();
                    if (taskItemDto != null){
                        taskItemDto.Members = (await multiResult.ReadAsync<MemberTaskDto>()).ToList();
                        if (taskItemDto.Members != null){
                            // Find member request extend due date
                            foreach (var member in taskItemDto.Members){
                                if (member.Requested )
                                    taskItemDto.MemberExtendDueDate = member;
                            }
                        }
                        taskItemDto.Comments = (await multiResult.ReadAsync<CommentDto>()).ToList();
                        taskItemDto.Subtasks = (await multiResult.ReadAsync<SubtaskDto>()).ToList();
                    }
                }

                // TaskItemDto taskItemDto =await _dapperContext.GetFirstAsync<TaskItemDto>(query, new {taskItemId});
                if (taskItemDto == null){
                    return new Response{
                        Message = "Not found task item",
                        IsSuccess = false
                    };
                }

                return new Response{
                    Message = "Get task item successfully",
                    Data = new Dictionary<string, object>{
                        ["taskItem"] = taskItemDto
                    },
                    IsSuccess = true
                };
            }
            catch (Exception e)
            {
                Console.WriteLine("GetTaskItemByIdAsync: " + e.Message);
                throw e;
            }
        }

        public async Task<Response> UpdateTaskItemAsync(int taskItemId, int workspaceId, string userId, TaskItemDto taskItemDto)
        {
            try
            {
                var query = @"SELECT Id, CardId, LabelId
                              FROM TaskItems WHERE Id = @taskItemId;";
                TaskItem taskItem = await _dapperContext.GetFirstAsync<TaskItem>(query, new {taskItemId});
                if (taskItem == null){
                    return new Response{
                        Message = "Not found task item",
                        IsSuccess = false
                    };
                }

                // Update task item to database
                var queryUpdate = @"UPDATE TaskItems SET Title = @title, Description = @description, DueDate = @dueDate,
                                    LabelId = @labelId, CardId = @cardId, Priority = @priority
                                    WHERE Id = @taskItemId";
                var parameters = new DynamicParameters();
                parameters.Add("title", taskItemDto.Title, DbType.String);  
                parameters.Add("description", taskItemDto.Description, DbType.String);  
                parameters.Add("dueDate", taskItemDto.DueDate, DbType.DateTime);  
                parameters.Add("cardId", taskItemDto.CardId, DbType.Int32);  
                parameters.Add("priority", taskItemDto.Priority, DbType.Int32);  
                parameters.Add("taskItemId", taskItemId, DbType.Int32);  
                
                var isUpdated = await _dapperContext.UpdateAsync(queryUpdate, parameters);
                if(isUpdated){
                    return new Response{
                        Message = "Updated task item is succeed",
                        IsSuccess = true
                    };
                }
                return new Response{
                    Message = "Updated task item is failed",
                    IsSuccess = false
                };
            }
            catch (Exception e)
            {
                Console.WriteLine("UpdateTaskItemAsync: " + e.Message);
                throw e;
            }
        }

        public async Task<Response> DeleteTaskItemAsync(int taskItemId, int workspaceId, string userId)
        {
            try{
                var taskItem = _dataContext.TaskItems.FirstOrDefault(t => t.Id == taskItemId);
                _dataContext.TaskItems.Remove(taskItem);
                var activation = new Activation{
                    UserId = userId,
                    WorkspaceId = workspaceId,
                    Content = $"Remove task {taskItem.Title}",
                    CreateAt = DateTime.Now
                };
                await _dataContext.Activations.AddAsync(activation);
                
                var isDeleted = await SaveChangeAsync();

                if (isDeleted){
                    return new Response{
                        Message = "Deleted task item is succeed",
                        IsSuccess = true
                    };
                }
                return new Response{
                    Message = "Deleted task item is failed",
                    IsSuccess = false
                };
            }
            catch (Exception e){
                Console.WriteLine("DeleteTaskItemAsync: " + e.Message);
                throw e;
            }
        }

        public async Task<bool> SaveChangeAsync()
        {
            return await _dataContext.SaveChangesAsync()>0;
        }

        public async Task<Response> UploadFileAsync(int taskItemId, int workspaceId, string userId, IFormFile file)
        {
            try{
                FileStream fs;
                FileStream ms = null;

                string path = "./FileUpload/File/";
                try
                {
                    var fileName = "file" + Path.GetExtension(file.FileName);
                    using (fs = new FileStream(Path.Combine(path, fileName), FileMode.Create))
                    {
                        await file.CopyToAsync(fs);
                    }
                    ms = new FileStream(Path.Combine(path, fileName), FileMode.Open);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }

                var fileUrl = await _webService.UploadFileToFirebase(ms, "Files", file.FileName);

                // Update file to db
                var taskItem = _dataContext.TaskItems.FirstOrDefault(t => t.Id == taskItemId);
                taskItem.Attachment = fileUrl;
                _dataContext.TaskItems.Update(taskItem);
                var activation = new Activation{
                    UserId = userId,
                    WorkspaceId = workspaceId,
                    Content = $"Upload file {file.FileName} to task {taskItem.Title}",
                    CreateAt = DateTime.Now
                };
                await _dataContext.Activations.AddAsync(activation);

                var isUpdated = await SaveChangeAsync();

                if(isUpdated){
                   
                    return new Response{
                        Message = "Upload file is succeed",
                        Data = new Dictionary<string, object>{
                            ["fileUrl"] = fileUrl
                        },
                        IsSuccess = true
                    };
                }
                return new Response{
                    Message = "Updated file is failed",
                    IsSuccess = false
                };
                
            }
            catch (Exception e){
                Console.WriteLine("UploadFileAsync: " + e.Message);
                throw e;
            }
        }

        public async Task<Response> PatchTaskItemAsync(int taskItemId, int workspaceId, string userId, JsonPatchDocument<TaskItem> patchTaskItem)
        {
            try{
                // Check user have permission to assign
                var mwAdmin = _dataContext.MemberWorkspaces.FirstOrDefault(
                    x => x.WorkspaceId == workspaceId &&
                    x.UserId == userId);                 
                if(mwAdmin.Role == ROLE_ENUM.Member)
                    return new Response{
                        Message = "Bạn không được phép chỉnh sửa nhiệm vụ",
                        IsSuccess = false
                    };

                var taskItem = _dataContext.TaskItems.FirstOrDefault(c => c.Id == taskItemId);
                patchTaskItem.ApplyTo(taskItem);

                _dataContext.TaskItems.Update(taskItem);

                var activation = new Activation{
                    UserId = userId,
                    WorkspaceId = workspaceId,
                    Content = $"Edit task {taskItem.Title}",
                    CreateAt = DateTime.Now
                };
                await _dataContext.Activations.AddAsync(activation);

                var isUpdated = await SaveChangeAsync();
                if (isUpdated){

                    var activationDto = _mapper.Map<Activation, ActivationDto>(activation);
    	            await _hubService.Clients.Group($"Workspace-{workspaceId}").SendActivationAsync(activationDto);
                    return new Response{
                        Message = "Updated file is succeed",
                        IsSuccess = true
                    };
                }
                return new Response{
                    Message = "Updated file is failed",
                    IsSuccess = false
                };
            }
            catch (Exception e){
                Console.WriteLine("PatchTaskItemAsync: " + e.Message);
                throw e;
            }
        }

        public async Task<Response> MoveTaskItemAsync(int taskItemId, int workspaceId, string userId, MoveTaskDto moveTaskDto)
        {
            try{           
                var after = moveTaskDto.After;
                var before = moveTaskDto.Before;
                Card cardAfter = null;
                bool isUpdated = false;
                Activation activation = null;
                var taskItem = _dataContext.TaskItems.FirstOrDefault(t => t.Id == taskItemId);
                // If task move to another card
                if(after["cardId"] != before["cardId"])
                {
                    taskItem.CardId = after["cardId"];
                    _dataContext.TaskItems.Update(taskItem);

                    // Remove index of task item from card before
                    var cardBefore = _dataContext.Cards.FirstOrDefault(c => c.Id == before["cardId"]);
                    if(cardBefore.TaskOrder.Length <= 3)
                        cardBefore.TaskOrder = "";
                    else{
                        var check = cardBefore.TaskOrder.Contains($"{taskItemId},");
                        cardBefore.TaskOrder = check?cardBefore.TaskOrder.Replace($"{taskItemId},",""): 
                                                    cardBefore.TaskOrder.Replace($",{taskItemId}","");
                    }

                    // Add index of task item from card after                    
                    cardAfter = _dataContext.Cards.FirstOrDefault(c => c.Id == after["cardId"]);
                    cardAfter.TaskOrder = InsertItemByIndex(cardAfter.TaskOrder, taskItemId, after["index"]);

                    _dataContext.Cards.UpdateRange(cardBefore, cardAfter);
                    
                    activation = new Activation{
                        UserId = userId,
                        WorkspaceId = workspaceId,
                        Content = $"Move task {taskItem.Title} from card {cardBefore.Name} to card {cardAfter.Name}",
                        CreateAt = DateTime.Now
                    };
                    await _dataContext.Activations.AddAsync(activation);

                    isUpdated = await SaveChangeAsync();
                    if (isUpdated){
                        return new Response{
                            Message = "Updated task is succeed",
                            Data = new Dictionary<string,object>{
                                ["taskItem"] = _mapper.Map<TaskItem, TaskItemDto>(taskItem),
                            },
                            IsSuccess = true
                        };              
                    }
                    return new Response{
                        Message = "Updated task is failed",                
                        IsSuccess = false
                    }; 
                }
                cardAfter = _dataContext.Cards.FirstOrDefault(c => c.Id == after["cardId"]);
                cardAfter.TaskOrder = InsertItemByIndex(cardAfter.TaskOrder, taskItemId, after["index"], before["index"]);

                _dataContext.Cards.Update(cardAfter);

                activation = new Activation{
                    UserId = userId,
                    WorkspaceId = workspaceId,
                    Content = $"Move task {taskItem.Title} in card {cardAfter.Name}",
                    CreateAt = DateTime.Now
                };
                await _dataContext.Activations.AddAsync(activation);

                isUpdated = await SaveChangeAsync();
                if (isUpdated){
                    return new Response{
                        Message = "Updated task is succeed",
                        Data = new Dictionary<string,object>{
                            ["taskItem"] = _mapper.Map<TaskItem, TaskItemDto>(taskItem),
                        },
                        IsSuccess = true
                    };              
                }
                return new Response{
                    Message = "Updated task is failed",                
                    IsSuccess = false
                }; 
            }
            catch(Exception e){
                Console.WriteLine("MoveTaskItemAsync: " + e.Message);
                throw e;
            }
        }

        private string InsertItemByIndex(string indexes,int item ,int newIndex,int oldIndex = -1){
            List<int> listIndex = null;
            if (indexes == ""){
                listIndex = new List<int>();
            }
            else{
                listIndex = JsonConvert.DeserializeObject<List<int>>(indexes);
            }
            if(oldIndex != -1)
                listIndex.RemoveAt(oldIndex);

            listIndex.Insert(newIndex, item);
            string rs = JsonConvert.SerializeObject(listIndex);
            return rs;
        }

        #region Member is assigned in task item
        public async Task<Response> GetTasksItemByMemberAsync(string memberId)
        {
            try
            {
                var query = @"SELECT t.Id, Title, Priority, DueDate, IsComplete, SubtaskQuantity, SubtaskCompleted,
                                     u.Id as CreatorId, u.FullName, u.Avatar, u.Email
                              FROM TaskItems t 
                              INNER JOIN aspnetusers u on u.Id = t.CreatorId
                              INNER JOIN MemberTasks mt on mt.TaskItemId = t.Id
                              WHERE mt.UserId = @memberId";

                var parameters = new DynamicParameters();
                parameters.Add("memberId", memberId, DbType.String);  

                List<TaskItemDto> taskItemDtos = await _dapperContext.GetListAsync<TaskItemDto>(query, parameters);

                return new Response{
                    Message = "Lấy danh sách nhiệm vụ thành công",
                    Data = new Dictionary<string, object>{
                        ["TaskItems"] = taskItemDtos
                    },
                    IsSuccess = true
                };
            }
            catch (Exception e)
            {
                Console.WriteLine("GetTasksItemByMemberAsync: " + e.Message);
                throw e;
            }
        }

        public async Task<Response> AssignMemberAsync(int taskItemId, int workspaceId, string userId, List<MemberTaskDto> memberTaskDtos)
        {
            try{
                // Check user have permission to assign
                var mwAdmin = _dataContext.MemberWorkspaces.FirstOrDefault(
                    x => x.WorkspaceId == workspaceId &&
                    x.UserId == userId);                 
                if(mwAdmin.Role == ROLE_ENUM.Member)
                    return new Response{
                        Message = "Bạn không được phép gán thành viên",
                        IsSuccess = false
                    };
                
                var membersNew = _mapper.Map<List<MemberTaskDto>, List<MemberTask>>(memberTaskDtos);
                List<MemberTask> membersOld = _dataContext.MemberTasks.Where(x => x.TaskItemId == taskItemId).ToList();
                // Check member new has been members old
                foreach (var memberTask in membersNew){
                    if (membersOld.Contains(memberTask))
                        membersOld.Remove(memberTask);
                    else
                        _dataContext.MemberTasks.Add(memberTask);
                }
                // Remove the old members
                foreach (var memberTask in membersOld){
                    _dataContext.MemberTasks.Remove(memberTask);
                }

                var isSaved = await SaveChangeAsync();

                var members = _dataContext.MemberTasks.Where(m => m.TaskItemId == taskItemId).ToList();
                return new Response{
                    Message = "Gán thành viên vào nhiệm vụ thành công",
                    Data = new Dictionary<string,object>{
                            ["Members"] = _mapper.Map<List<MemberTask>, List<MemberTaskDto>>(members),
                        },
                    IsSuccess = true
                };
                
            }
            catch (Exception e){
                Console.WriteLine("AssignMemberAsync: " + e.Message);
                throw e;
            }
        }

        public async Task<Response> RemoveMemberAsync(int workspaceId, string userId, MemberTaskDto memberTaskDto)
        {
            try{
                var memberTask = _dataContext.MemberTasks.FirstOrDefault(
                    t => t.TaskItemId == memberTaskDto.TaskItemId 
                    && t.UserId == memberTaskDto.UserId);
                if(memberTask == null)
                    return new Response{
                        Message = "Member is not found",
                        IsSuccess = false
                    };
                
                _dataContext.MemberTasks.Remove(memberTask);

                var isSaved = await SaveChangeAsync();

                if (isSaved){
                    return new Response{
                        Message = "Deleted member to task is succeed",
                        IsSuccess = true
                    };
                }
                return new Response{
                    Message = "Deleted member to task is failed",
                    IsSuccess = false
                };
            }
            catch (Exception e){
                Console.WriteLine("RemoveMemberAsync: " + e.Message);
                throw e;
            }
        }

        public async Task<Response> ExtendDueDateMemberAsync(int workspaceId, string userId, MemberTaskDto memberTaskDto)
        {
            try{
                // Check user have permission to assign
                var mwAdmin = _dataContext.MemberWorkspaces.FirstOrDefault(
                    x => x.WorkspaceId == workspaceId &&
                    x.UserId == userId);                 
                if(mwAdmin.Role != ROLE_ENUM.Member)
                    return new Response{
                        Message = "Chức này chỉ giành cho thành viên",
                        IsSuccess = false
                    };

                var memberTask = _dataContext.MemberTasks.FirstOrDefault(
                    m => m.TaskItemId == memberTaskDto.TaskItemId
                    && m.UserId == userId);

                memberTask.ExtendDate = memberTaskDto.ExtendDate;
                memberTask.Requested = true;
                     
                _dataContext.MemberTasks.Update(memberTask);

                var taskItem = _dataContext.TaskItems.FirstOrDefault(t => t.Id == memberTaskDto.TaskItemId);
                
                var activation = new Activation{
                    UserId = userId,
                    WorkspaceId = workspaceId,
                    Content = $"Thành viên yêu cầu gia hạn vào {memberTask.ExtendDate.Value.ToShortDateString()} trong nhiệm vụ {taskItem.Title}",
                    CreateAt = DateTime.Now
                };
                _dataContext.Activations.Add(activation);

                var isSaved = await SaveChangeAsync();

                if (isSaved){
                    var activationDto = _mapper.Map<Activation, ActivationDto>(activation);
    	            await _hubService.Clients.Group($"Workspace-{workspaceId}").SendActivationAsync(activationDto);
                    return new Response{
                        Message = "Yêu cầu gia hạn thành công",
                        IsSuccess = true
                    };
                }
                return new Response{
                    Message = "Yêu cầu gia hạn thất bại",
                    IsSuccess = false
                };
            }
            catch (Exception e){
                Console.WriteLine("CreateCommentAsync: " + e.Message);
                throw e;
            }
        }

        public Task<Response> ConfirmExtendMemberAsync(int workspaceId, string userId, MemberTaskDto memberTaskDto)
        {
            throw new NotImplementedException();
        }
        public Task<Response> SortingTasksItemByMemberAsync(string memberId)
        {
            throw new NotImplementedException();
        }

        public Task<Response> FilteringTasksItemByMemberAsync(string memberId)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region User comment in task item
        public async Task<Response> CreateCommentAsync(int workspaceId, string userId, CommentDto commentDto)
        {
            try{
                var taskItem = _dataContext.TaskItems.FirstOrDefault(t => t.Id == commentDto.TaskItemId);
                if(taskItem == null)
                    return new Response{
                        Message = "Task is not found",
                        IsSuccess = false
                    };
                
                var comment = _mapper.Map<CommentDto, Comment>(commentDto);
                comment.UserId = userId;
                     
                _dataContext.Comments.Add(comment);
                
                var activation = new Activation{
                    UserId = userId,
                    WorkspaceId = workspaceId,
                    Content = $"Member comment in task {taskItem.Title}",
                    CreateAt = DateTime.Now
                };
                _dataContext.Activations.Add(activation);

                var isSaved = await SaveChangeAsync();

                if (isSaved){
                    var activationDto = _mapper.Map<Activation, ActivationDto>(activation);
    	            await _hubService.Clients.Group($"Workspace-{workspaceId}").SendActivationAsync(activationDto);
                    return new Response{
                        Message = "Comment in task is succeed",
                        IsSuccess = true
                    };
                }
                return new Response{
                    Message = "Comment in task is failed",
                    IsSuccess = false
                };
            }
            catch (Exception e){
                Console.WriteLine("CreateCommentAsync: " + e.Message);
                throw e;
            }
        }

        public Task<Response> EditCommentAsync(int WorkspaceId, string userId, CommentDto commentDto)
        {
            throw new NotImplementedException();
        }

        public Task<Response> DeleteCommentAsync(int taskItemId, int WorkspaceId, string userId, int userTaskId)
        {
            throw new NotImplementedException();
        }

        #endregion
        public Task<Response> AddLabelToTaskItemAsync(int workspaceId, string userId, List<LabelDto> labelDto)
        {
            throw new NotImplementedException();
        }

    }
}