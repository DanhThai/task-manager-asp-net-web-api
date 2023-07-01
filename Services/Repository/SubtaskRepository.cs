
using AutoMapper;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.SignalR;
using TaskManager.API.Data;
using TaskManager.API.Data.DTOs;
using TaskManager.API.Data.Models;
using TaskManager.API.Helper;
using TaskManager.API.Services.IRepository;

namespace TaskManager.API.Services.Repository
{
    
    public class SubtaskRepository : ISubtaskRepository
    {
        private readonly DataContext _dataContext;
        private readonly IMapper _mapper;
        private readonly DapperContext _dapperContext;
        private IHubContext<HubService, IHubService> _hubService;
        private readonly GetData _getData;

        public SubtaskRepository(DataContext dataContext, IMapper mapper, DapperContext dapperContext, IHubContext<HubService, IHubService> hubService, GetData getData)
        {
            _dataContext = dataContext;
            _mapper = mapper;
            _dapperContext = dapperContext;
            _hubService = hubService;
            _getData = getData;
        }
        public async Task<Response> CreateSubtaskAsync(int workspaceId, string userId, SubtaskDto subtaskDto)
        {
            try
            {
                var subtask = _mapper.Map<SubtaskDto, Subtask>(subtaskDto);
                var taskItem = _dataContext.TaskItems.FirstOrDefault(t => t.Id == subtaskDto.TaskItemId);
                if (taskItem == null)
                    return new Response
                    {
                        Message = "Không tìm thấy nhiệm vụ",
                        IsSuccess = false
                    };
                _dataContext.Subtasks.Add(subtask);

                taskItem.SubtaskQuantity += 1;
                _dataContext.TaskItems.Update(taskItem);

                var activation = new Activation
                {
                    UserId = userId,
                    WorkspaceId = workspaceId,
                    Content = $"đã tạo nhiệm vụ con {subtask.Name} ở trong nhiệm vụ {taskItem.Title}",
                    CreateAt = DateTime.Now
                };
                await _dataContext.Activations.AddAsync(activation);

                var isSaved = await SaveChangeAsync();
                if (isSaved)
                {
                    // Send SignalR
                    var workspaceDto = await _getData.GetWorkspaceById(workspaceId, userId);
                    await _hubService.Clients.Group($"workspace-{workspaceId}").WorkspaceAsync(workspaceDto);
                    var resTaskItemDto = await _getData.GetTaskItemById(subtask.TaskItemId);
                    await _hubService.Clients.Group($"taskItem-{subtask.TaskItemId}").TaskItemAsync(resTaskItemDto);
                    
                    subtaskDto = _mapper.Map<Subtask, SubtaskDto>(subtask);
                    return new Response
                    {
                        Message = "Tạo nhiệm vụ con thành công",
                        Data = new Dictionary<string, object>{
                            ["Subtask"] = subtaskDto,
                        }, 
                        IsSuccess = true
                    };
                }
                return new Response
                {
                    Message = "Tạo nhiệm vụ con thất bại",
                    IsSuccess = false
                };
            }
            catch (Exception e)
            {
                Console.WriteLine("CreateSubtaskAsync " + e.Message);
                throw e;
            }
        }

        public async Task<Response> DeleteSubtaskAsync(int subtaskId, int workspaceId, string userId)
        {
            try
            {
                var subtask = _dataContext.Subtasks.FirstOrDefault(t => t.Id == subtaskId);
                if (subtask != null)
                {
                    _dataContext.Subtasks.Remove(subtask);

                    var taskItem = _dataContext.TaskItems.FirstOrDefault(c => c.Id == subtask.TaskItemId);
                    taskItem.SubtaskQuantity -= 1;
                    if(subtask.Status)
                        taskItem.SubtaskCompleted -= 1;
                    _dataContext.TaskItems.Update(taskItem);

                    var activation = new Activation
                    {
                        UserId = userId,
                        WorkspaceId = workspaceId,
                        Content = $"đã xóa nhiệm vụ con {subtask.Name} trong {taskItem.Title}",
                        CreateAt = DateTime.Now
                    };
                    await _dataContext.Activations.AddAsync(activation);

                    var isSaved = await SaveChangeAsync();
                    if (isSaved)
                    {
                        // Send SignalR
                        var workspaceDto = await _getData.GetWorkspaceById(workspaceId, userId);
                        await _hubService.Clients.Group($"workspace-{workspaceId}").WorkspaceAsync(workspaceDto);
                        var resTaskItemDto = await _getData.GetTaskItemById(subtask.TaskItemId);
                        await _hubService.Clients.Group($"taskItem-{subtask.TaskItemId}").TaskItemAsync(resTaskItemDto);

                        return new Response
                        {
                            Message = "Xóa nhiệm vụ con thành công",
                            IsSuccess = true
                        };
                    }
                    return new Response
                    {
                        Message = "Xóa nhiệm vụ con thất bại",
                        IsSuccess = false
                    };
                }
                return new Response
                {
                    Message = "Không tìm thấy nhiệm vụ con",
                    IsSuccess = false
                };

            }
            catch (Exception e)
            {
                Console.WriteLine("DeleteSubtaskAsync " + e.Message);
                throw e;
            }
        }

        public Task<Response> GetSubtaskByIdAsync(int subtaskId)
        {
            throw new NotImplementedException();
        }

        public async Task<Response> PatchSubtaskAsync(int subtaskId, int WorkspaceId, string userId, JsonPatchDocument<Subtask> patchSubtask)
        {
            try
            {
                var subtask = _dataContext.Subtasks.FirstOrDefault(t => t.Id == subtaskId);
                if (subtask == null)
                    return new Response
                    {
                        Message = "Không tìm thấy nhiệm vụ con.",
                        IsSuccess = false
                    };
                patchSubtask.ApplyTo(subtask);

                if (patchSubtask.Operations[0].path.Contains("status")){
                    var taskItem = _dataContext.TaskItems.FirstOrDefault(c => c.Id == subtask.TaskItemId);
                    if((bool)patchSubtask.Operations[0].value){
                        taskItem.SubtaskCompleted += 1;
                        taskItem.IsComplete = taskItem.SubtaskCompleted == taskItem.SubtaskQuantity;
                    }
                    else
                    {
                        taskItem.IsComplete = false;
                        taskItem.SubtaskCompleted = taskItem.SubtaskCompleted>0 ? taskItem.SubtaskCompleted-1: 0 ;
                    }
                    if(taskItem.SubtaskCompleted <= taskItem.SubtaskQuantity && taskItem.SubtaskCompleted >= 0){
                        _dataContext.TaskItems.Update(taskItem);
                        var activation = new Activation
                        {
                            UserId = userId,
                            WorkspaceId = WorkspaceId,
                            Content = $"đã cập nhật trạng thái nhiệm vụ con {subtask.Name} trong {taskItem.Title}",
                            CreateAt = DateTime.Now
                        };
                    }
                }
                else{
                    var activation = new Activation
                        {
                            UserId = userId,
                            WorkspaceId = WorkspaceId,
                            Content = $"đã cập nhật chỉnh sửa nhiệm vụ con {subtask.Name}",
                            CreateAt = DateTime.Now
                        };
                }

                _dataContext.Subtasks.Update(subtask);


                var isSaved = await SaveChangeAsync();
                if (isSaved)
                {
                    // Send SignalR
                    var workspaceDto = await _getData.GetWorkspaceById(WorkspaceId, userId);
                    await _hubService.Clients.Group($"workspace-{WorkspaceId}").WorkspaceAsync(workspaceDto);
                    var resTaskItemDto = await _getData.GetTaskItemById(subtask.TaskItemId);
                    await _hubService.Clients.Group($"taskItem-{subtask.TaskItemId}").TaskItemAsync(resTaskItemDto);
                    
                    return new Response
                    {
                        Message = "Cập nhật nhiệm vụ con thành công",
                        IsSuccess = true
                    };
                }
                return new Response
                {
                    Message = "Cập nhật nhiệm vụ con thất bại",
                    IsSuccess = false
                };

            }
            catch (Exception e)
            {
                Console.WriteLine("UpdateChecklistAsync " + e.Message);
                throw e;
            }
        }

        public async Task<bool> SaveChangeAsync()
        {
            return await _dataContext.SaveChangesAsync() > 0;        
        }

        public async Task<Response> UpdateSubtaskAsync(int subtaskId, int workspaceId, string userId, SubtaskDto subtaskDto)
        {
            try
            {
                var subtask = _dataContext.Subtasks.FirstOrDefault(t => t.Id == subtaskId);
                if (subtask == null)
                    return new Response
                    {
                        Message = "Không tìm thấy nhiệm vụ con.",
                        IsSuccess = false
                    };
                subtask.Name = subtaskDto.Name;

                _dataContext.Subtasks.Update(subtask);

                var isSaved = await SaveChangeAsync();
                if (isSaved)
                {
                    var resTaskItemDto = await _getData.GetTaskItemById(subtask.TaskItemId);
                    await _hubService.Clients.Group($"taskItem-{subtask.TaskItemId}").TaskItemAsync(resTaskItemDto);
                    return new Response
                    {
                        Message = "Cập nhật nhiệm vụ con thành công.",
                        IsSuccess = true
                    };
                }
                return new Response
                {
                    Message = "Cập nhật nhiệm vụ con thất bại.",
                    IsSuccess = false
                };

            }
            catch (Exception e)
            {
                Console.WriteLine("UpdateChecklistAsync " + e.Message);
                throw e;
            }
        }
    }
}