using System.Data;
using AutoMapper;
using Dapper;
using Microsoft.AspNetCore.SignalR;
using TaskManager.API.Data;
using TaskManager.API.Data.DTOs;
using TaskManager.API.Data.Models;
using TaskManager.API.Services.IRepository;

namespace TaskManager.API.Services.Repository
{
    public class ChecklistRepository : IChecklistRepository
    {
        private readonly DataContext _dataContext;
        private readonly IMapper _mapper;
        private readonly DapperContext _dapperContext;
        private IHubContext<HubService, IHubService> _hubService;
        public ChecklistRepository(DataContext dataContext, IMapper mapper, DapperContext dapperContext, IHubContext<HubService, IHubService> hubService)
        {
            _dataContext = dataContext;
            _mapper = mapper;
            _dapperContext = dapperContext;
            _hubService = hubService;
        }

        public async Task<Response> CreateChecklistAsync(int workspaceId, string userId, ChecklistDto checklistDto)
        {
            try
            {
                var checklist = _mapper.Map<ChecklistDto, Checklist>(checklistDto);
                var taskItem = _dataContext.TaskItems.FirstOrDefault(t => t.Id == checklistDto.Id);
                var checklistExist = _dataContext.Checklists.FirstOrDefault(t => t.Id == checklistDto.Id);
                if (taskItem == null || checklistExist != null)
                    return new Response
                    {
                        Message = "Task item invalid or one task item can create only one checklist",
                        IsSuccess = false
                    };
                    
                _dataContext.Checklists.Add(checklist);

                var activation = new Activation
                {
                    UserId = userId,
                    WorkspaceId = workspaceId,
                    Content = $"Create checklist {checklist.Name} in task {taskItem.Title}",
                    CreateAt = DateTime.Now
                };
                await _dataContext.Activations.AddAsync(activation);

                var isSaved = await SaveChangeAsync();
                if (isSaved)
                {
                    var activationDto = _mapper.Map<Activation, ActivationDto>(activation);
                    await _hubService.Clients.Group($"Workspace-{workspaceId}").SendActivationAsync(activationDto);

                    checklistDto = _mapper.Map<Checklist, ChecklistDto>(checklist);
                    return new Response
                    {
                        Message = "Created checklist is succeed",
                        Data = new Dictionary<string, object>{
                            ["Checklist"] = checklistDto,
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
                Console.WriteLine("CreateChecklistAsync " + e.Message);
                throw e;
            }
        }

        public async Task<Response> DeleteChecklistAsync(int checklistId, int workspaceId, string userId)
        {
            try
            {
                var checklist = _dataContext.Checklists.FirstOrDefault(t => t.Id == checklistId);
                if (checklist != null)
                {
                    _dataContext.Checklists.Remove(checklist);

                    var activation = new Activation
                    {
                        UserId = userId,
                        WorkspaceId = workspaceId,
                        Content = $"Remove checklist {checklist.Name} in task {checklist.TaskItem.Title}",
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
                            Message = "Deleted checklist is succeed",
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
                    Message = "Not Found checklist",
                    IsSuccess = false
                };

            }
            catch (Exception e)
            {
                Console.WriteLine("DeleteChecklistAsync " + e.Message);
                throw e;
            }
        }

        public async Task<Response> GetChecklistByIdAsync(int checklistId)
        {
            try
            {
                var query = @"SELECT Id, Name, Status
                              FROM Checklits  WHERE Id = @checklistId;" +
                            @"SELECT Id, Name, Status
                              FROM Subtasks   
                              WHERE ChecklistId = @checklistId;";
                var parameters = new DynamicParameters();
                parameters.Add("checklistId", checklistId, DbType.Int32);  

                ChecklistDto checklistDto = null;
                using (var connection = _dapperContext.CreateConnection())
                using(var multiResult = await connection.QueryMultipleAsync(query, parameters))
                {
                    checklistDto = await multiResult.ReadSingleOrDefaultAsync<ChecklistDto>();
                    if (checklistDto != null){
                        checklistDto.Subtasks = (await multiResult.ReadAsync<SubtaskDto>()).ToList();
                    }
                }
                if (checklistDto != null)
                    return new Response{
                        Message = "Get checklist successfully",
                        Data = new Dictionary<string, object>{
                            ["Checklist"] = checklistDto
                        },
                        IsSuccess = true
                    };
                return new Response
                {
                    Message = "Checklist is not found",
                    IsSuccess = false
                };
            }
            catch (Exception e)
            {
                Console.WriteLine("GetChecklistByIdAsync " + e.Message);
                throw e;
            }
        }

        public async Task<bool> SaveChangeAsync()
        {
            return await _dataContext.SaveChangesAsync() > 0;
        }

        public async Task<Response> UpdateChecklistAsync(int checklistId, int workspaceId, string userId, ChecklistDto checklistDto)
        {
            try
            {
                var checklist = _dataContext.Checklists.FirstOrDefault(t => t.Id == checklistId);
                checklist.Name = checklistDto.Name;

                _dataContext.Checklists.Update(checklist);

                var activation = new Activation
                {
                    UserId = userId,
                    WorkspaceId = workspaceId,
                    Content = $"Update checklist {checklist.Name} in task {checklist.TaskItem.Title}",
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
                        Message = "Update checklist is succeed",
                        IsSuccess = true
                    };
                }
                return new Response
                {
                    Message = "Remove checklist is failed",
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