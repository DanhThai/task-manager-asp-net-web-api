using System.Data;
using AutoMapper;
using Dapper;
using Microsoft.AspNetCore.JsonPatch;
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


        public TaskItemRepository(DataContext dataContext, IMapper mapper, DapperContext dapperContext, IWebService webService)
        {
            _dataContext = dataContext;
            _mapper = mapper;
            _dapperContext = dapperContext;
            _webService = webService;
        }

        public async Task<Response> CreateTaskItemAsync(TaskItemDto taskItemDto, string userId)
        {
            try{
                taskItemDto.Id = 0;
                // taskItemDto.LabelId = 1;

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
                    _dataContext.Cards.Update(card);
                    isSaved = await SaveChangeAsync();

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
                var query = @"SELECT Id, Title, Description, Attachment, Priority, DueDate, CardId
                              FROM TaskItems t WHERE t.Id = @taskItemId;" +
                              @"SELECT u.Id, u.FullName, u.Email, u.Avatar, ut.Comment, ut.Assigned,  ut.IsCreator
                              FROM aspnetusers u  
                              INNER JOIN UserTasks ut on u.Id = ut.UserId 
                              WHERE ut.TaskItemId = @taskItemId;";
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
                            if(user.Assigned)
                                if(taskItemDto.Assigns == null)
                                    taskItemDto.Assigns = new List<UserTaskDto>();
                                taskItemDto.Assigns.Add(user);
                            
                            if(user.Comment != null)
                                if(taskItemDto.Comments == null)
                                    taskItemDto.Comments = new List<UserTaskDto>();
                                taskItemDto.Comments.Add(user);
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

        public async Task<Response> UpdateTaskItemAsync(TaskItemDto taskItemDto, int taskItemId)
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

        public async Task<Response> DeleteTaskItemAsync(int taskItemId)
        {
            try{
                var query = @"DELETE FROM TaskItems WHERE Id = @taskItemId;";
                var isDeleted = await _dapperContext.DeleteAsync(query, new {taskItemId});

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

        public async Task<Response> UploadFileAsync(int taskItemId, IFormFile file)
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
                var queryUpdate = @"UPDATE TaskItems SET Attachment = @attachment WHERE Id = @taskItemId";
                var parameters = new DynamicParameters();
                parameters.Add("attachment", fileUrl, DbType.String); 
                parameters.Add("taskItemId", taskItemId, DbType.Int32);  
                
                var isUpdated = await _dapperContext.UpdateAsync(queryUpdate, parameters);
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

        public async Task<Response> PatchTaskItemAsync(JsonPatchDocument<TaskItem> patchTaskItem, int taskItemId)
        {
            var taskItem = _dataContext.TaskItems.FirstOrDefault(c => c.Id == taskItemId);
            patchTaskItem.ApplyTo(taskItem);

            _dataContext.TaskItems.Update(taskItem);
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
    }
}