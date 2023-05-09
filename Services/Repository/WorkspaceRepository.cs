
using Dapper;
using AutoMapper;
using TaskManager.API.Data;
using TaskManager.API.Data.DTOs;
using TaskManager.API.Data.Models;
using TaskManager.API.Services.IRepository;
using System.Data;
using Newtonsoft.Json;

namespace TaskManager.API.Services.Repository
{
    public class WorkspaceRepository : IWorkspaceRepository
    {
        private readonly DataContext _dataContext;
        private readonly IMapper _mapper;
        private readonly DapperContext _dapperContext;


        public WorkspaceRepository(DataContext dataContext, IMapper mapper, DapperContext dapperContext)
        {
            this._dataContext = dataContext;
            this._mapper = mapper;
            _dapperContext = dapperContext;
        }

        public async Task<Response> CreateWorkspaceAsync(WorkspaceDto workspaceDto, string userId, string userName)
        {
            workspaceDto.Id = null;
            Workspace workspace = _mapper.Map<WorkspaceDto, Workspace>(workspaceDto);
            workspace.CreateAt = DateTime.UtcNow;
            workspace.CreatorId = userId;
            workspace.CreatorName = userName;
            workspace.Cards = new List<Card>{
                new Card("Todo", 0),
                new Card("Doing", 1),
                new Card("Done", 2),
            };
            var wsCreated = await _dataContext.Workspaces.AddAsync(workspace);

            UserWorkspace userWorkspace = new UserWorkspace{
                                                UserId = userId,
                                                IsOwner = true};
            userWorkspace.Workspace = workspace;
            await _dataContext.UserWorkspaces.AddAsync(userWorkspace);   

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

        public async Task<Response> GetWorkspaceByIdAsync(int workspaceId, string userId)
        {
            try{
                // Get the workspace by Id from the database
                // var query = @"SELECT w.Id, Title, Description, Logo, Background, Permission, CreatorId, CreatorName, 
                //                      u.Id, u.FullName, u.Email, u.Avatar
                //               FROM Workspaces w
                //               INNER JOIN (
                //                 SELECT uw.WorkspaceId, u.Id, u.FullName, u.Email, u.Avatar
                //                 From aspnetusers u  
                //                 INNER JOIN UserWorkspaces uw on u.Id = uw.UserId 
                //                 WHERE uw.WorkspaceId = @WorkspaceId
                //               ) as u on w.Id = u.WorkspaceId
                //               WHERE w.Id = @WorkspaceId";

                var query = @"SELECT Id, Title, Description, Logo, Background, Permission, CreatorId, CreatorName 
                              FROM Workspaces w WHERE w.Id = @WorkspaceId;" +
                            @"SELECT u.Id, u.FullName, u.Email, u.Avatar
                              FROM aspnetusers u  
                              INNER JOIN UserWorkspaces uw on u.Id = uw.UserId 
                              WHERE uw.WorkspaceId = @WorkspaceId;";

                var parameters = new DynamicParameters();
                parameters.Add("WorkspaceId", workspaceId, DbType.Int32);  

                WorkspaceDto workspaceDto = null;
                using (var connection = _dapperContext.CreateConnection())
                using(var multiResult = await connection.QueryMultipleAsync(query, parameters))
                {
                    workspaceDto = await multiResult.ReadSingleOrDefaultAsync<WorkspaceDto>();
                    if (workspaceDto != null){
                        workspaceDto.Members = (await multiResult.ReadAsync<UserDto>()).ToList();
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
                                    t.Id, t.Title, t.Description, t.Priority, t.DueDate, t.CardId
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
                            var listTaskItem = JsonConvert.DeserializeObject<List<int>>(card.TaskOrder);
                            currentCard = card;
                            currentCard.ListTasksOrder = listTaskItem;
                            cardDict.Add(card.Id, currentCard);
                        }
                        if (taskItem != null)
                            currentCard.TaskItems.Add(taskItem);
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
        
        public async Task<Response> UpdateWorkspaceAsync(WorkspaceDto workspaceDto)
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
                workspace.UpdateAt = DateTime.UtcNow;

                _dataContext.Workspaces.Update(workspace);
                var save = await SaveChangeAsync();
                if (save){
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

    }
}