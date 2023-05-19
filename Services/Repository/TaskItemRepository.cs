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
                var a = await _dataContext.TaskItems.AddAsync(taskItem);

                // Add user created task item
                UserTask userTask = new UserTask(){
                    IsCreator = true,
                    UserId = userId
                };
                userTask.TaskItem = taskItem;
                await _dataContext.UserTasks.AddAsync(userTask);
            
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
                var query = @"SELECT Id, Title, Description, Attachment, Priority, DueDate, CardId, SubtaskQuantity, SubtaskCompleted
                              FROM TaskItems t WHERE t.Id = @taskItemId;" +
                            @"SELECT u.Id, u.FullName, u.Email, u.Avatar, ut.Comment, ut.Assigned,  ut.IsCreator
                              FROM aspnetusers u  
                              INNER JOIN UserTasks ut on u.Id = ut.UserId 
                              WHERE ut.TaskItemId = @taskItemId;"+
                            @"SELECT ID, Name, Status
                              FROM Checklists c  
                              WHERE c.Id = @taskItemId;"+
                            @"SELECT ID, Name, Status
                              FROM Subtasks  
                              WHERE ChecklistId = @taskItemId;";
                var parameters = new DynamicParameters();
                parameters.Add("taskItemId", taskItemId, DbType.Int32);  

                TaskItemDto taskItemDto = null;
                using (var connection = _dapperContext.CreateConnection())
                using(var multiResult = await connection.QueryMultipleAsync(query, parameters))
                {
                    taskItemDto = await multiResult.ReadSingleOrDefaultAsync<TaskItemDto>();
                    if (taskItemDto != null){
                        var users = (await multiResult.ReadAsync<UserTaskDto>()).ToList();
                        foreach (var user in users){
                            if(user.Assigned == true){
                                if(taskItemDto.Assigns == null)
                                    taskItemDto.Assigns = new List<UserTaskDto>();
                                taskItemDto.Assigns.Add(user);
                            }
                            
                            if(user.Comment != null)
                            {
                                if(taskItemDto.Comments == null)
                                    taskItemDto.Comments = new List<UserTaskDto>();
                                taskItemDto.Comments.Add(user);
                            }
                        }
                        taskItemDto.Checklist = await multiResult.ReadSingleOrDefaultAsync<ChecklistDto>();
                        if (taskItemDto.Checklist != null)
                        {
                            var subtasks = (await multiResult.ReadAsync<SubtaskDto>()).ToList();
                            taskItemDto.Checklist.Subtasks = subtasks;
                            // if (taskItemDto.Checklist.Subtasks != null)
                            // {
                            //     int subtaskQuantity = subtasks.Count();
                            //     int taskCompleted = 0;
                            //     for (int i = 0; i < subtaskQuantity; i++){
                            //         if( subtasks[i].Status == true)
                            //             taskCompleted += 1;
                            //     }                
                            //     taskItemDto.Checklist.Percentage = (taskCompleted/subtaskQuantity) * 100;
                            // }
                        }

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
                var isDeleted = await SaveChangeAsync();

                if (isDeleted){
                    var activation = new Activation{
                        UserId = userId,
                        WorkspaceId = workspaceId,
                        Content = $"Remove task {taskItem.Title}",
                        CreateAt = DateTime.Now
                    };
                    await _dataContext.Activations.AddAsync(activation);
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
    }
}