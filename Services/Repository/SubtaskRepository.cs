
using AutoMapper;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.SignalR;
using TaskManager.API.Data;
using TaskManager.API.Data.DTOs;
using TaskManager.API.Data.Models;
using TaskManager.API.Services.IRepository;

namespace TaskManager.API.Services.Repository
{
    
    public class SubtaskRepository : ISubtaskRepository
    {
        private readonly DataContext _dataContext;
        private readonly IMapper _mapper;
        private readonly DapperContext _dapperContext;
        private IHubContext<HubService, IHubService> _hubService;
        public SubtaskRepository(DataContext dataContext, IMapper mapper, DapperContext dapperContext, IHubContext<HubService, IHubService> hubService)
        {
            _dataContext = dataContext;
            _mapper = mapper;
            _dapperContext = dapperContext;
            _hubService = hubService;
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
                        Message = "Created subtask is failed",
                        IsSuccess = false
                    };
                _dataContext.Subtasks.Add(subtask);

                taskItem.SubtaskQuantity += 1;
                _dataContext.TaskItems.Update(taskItem);

                var activation = new Activation
                {
                    UserId = userId,
                    WorkspaceId = workspaceId,
                    Content = $"Create subtask {subtask.Name} in task {taskItem.Title}",
                    CreateAt = DateTime.Now
                };
                await _dataContext.Activations.AddAsync(activation);

                var isSaved = await SaveChangeAsync();
                if (isSaved)
                {
                    var activationDto = _mapper.Map<Activation, ActivationDto>(activation);
                    await _hubService.Clients.Group($"Workspace-{workspaceId}").SendActivationAsync(activationDto);

                    subtaskDto = _mapper.Map<Subtask, SubtaskDto>(subtask);
                    return new Response
                    {
                        Message = "Created subtask is succeed",
                        Data = new Dictionary<string, object>{
                            ["Subtask"] = subtaskDto,
                        }, 
                        IsSuccess = true
                    };
                }
                return new Response
                {
                    Message = "Created checklist is failed",
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
                        Content = $"Remove subtask {subtask.Name}",
                        CreateAt = DateTime.Now
                    };
                    await _dataContext.Activations.AddAsync(activation);

                    var isSaved = await SaveChangeAsync();
                    if (isSaved)
                    {
                        var activationDto = _mapper.Map<Activation, ActivationDto>(activation);
                        await _hubService.Clients.Group($"Workspace-{workspaceId}").SendActivationAsync(activationDto);

                        return new Response
                        {
                            Message = "Delete subtask is succeed",
                            IsSuccess = true
                        };
                    }
                    return new Response
                    {
                        Message = "Remove checklist is failed",
                        IsSuccess = false
                    };
                }
                return new Response
                {
                    Message = "Created checklist is failed",
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
                patchSubtask.ApplyTo(subtask);

                if (patchSubtask.Operations[0].path.Contains("Status")){
                    var taskItem = _dataContext.TaskItems.FirstOrDefault(c => c.Id == subtask.TaskItemId);
                    if((bool)patchSubtask.Operations[0].value){
                        taskItem.SubtaskCompleted += 1;
                    }
                    else
                        taskItem.SubtaskCompleted -= 1;
                    _dataContext.TaskItems.Update(taskItem);
                }

                _dataContext.Subtasks.Update(subtask);


                var isSaved = await SaveChangeAsync();
                if (isSaved)
                {
                    return new Response
                    {
                        Message = "Update subtask is succeed",
                        IsSuccess = true
                    };
                }
                return new Response
                {
                    Message = "Remove subtask is failed",
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
                subtask.Name = subtaskDto.Name;

                _dataContext.Subtasks.Update(subtask);

                var isSaved = await SaveChangeAsync();
                if (isSaved)
                {
                    return new Response
                    {
                        Message = "Update subtask is succeed",
                        IsSuccess = true
                    };
                }
                return new Response
                {
                    Message = "Remove subtask is failed",
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