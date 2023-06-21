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
            try
            {
                TaskItem taskItem = _mapper.Map<TaskItemDto, TaskItem>(taskItemDto);
                taskItem.CreatorId = userId;
                Console.WriteLine("userId", userId);
                var a = await _dataContext.TaskItems.AddAsync(taskItem);

                var isSaved = await SaveChangeAsync();
                if (isSaved)
                {
                    // Update tasks order of card
                    var card = _dataContext.Cards.FirstOrDefault(c => c.Id == taskItemDto.CardId);
                    List<int> listTaskItem = null;

                    if (card.TaskOrder != "")
                    {
                        listTaskItem = JsonConvert.DeserializeObject<List<int>>(card.TaskOrder);
                    }
                    else
                        listTaskItem = new List<int>();

                    listTaskItem.Add(taskItem.Id);
                    card.TaskOrder = JsonConvert.SerializeObject(listTaskItem);
                    card.TaskQuantity += 1;
                    _dataContext.Cards.Update(card);

                    var activation = new Activation
                    {
                        UserId = userId,
                        WorkspaceId = workspaceId,
                        Content = $"Create task {taskItem.Title} in card {card.Name}",
                        CreateAt = DateTime.Now
                    };
                    await _dataContext.Activations.AddAsync(activation);

                    var workspace = _dataContext.Workspaces.FirstOrDefault(x => x.Id == workspaceId);
                    workspace.TaskQuantity += 1;
                    workspace.IsComplete = false;
                    workspace.VisitDate = DateTime.Now;
                    _dataContext.Workspaces.Update(workspace);

                    isSaved = await SaveChangeAsync();

                    // Send changed card to client hub
                    var cardDto = _mapper.Map<Card, CardDto>(card);
                    var activationDto = _mapper.Map<Activation, ActivationDto>(activation);
                    await _hubService.Clients.Group($"Workspace-{workspaceId}").SendCardAsync(cardDto);
                    await _hubService.Clients.Group($"Workspace-{workspaceId}").SendActivationAsync(activationDto);

                    taskItemDto = _mapper.Map<TaskItem, TaskItemDto>(taskItem);
                    return new Response
                    {
                        Message = "Tạo nhiệm vụ thành công",
                        Data = new Dictionary<string, object>
                        {
                            ["TaskItem"] = taskItemDto,
                        },
                        IsSuccess = false
                    };
                }
                return new Response
                {
                    Message = "Tạo nhiệm vụ thất bại",
                    IsSuccess = false
                };
            }
            catch (Exception e)
            {
                Console.WriteLine("CreateTaskItemAsync: " + e.Message);
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
                            // Get member is assigned
                            @"SELECT mt.Id, u.Id as UserId, u.FullName, u.Avatar, u.Email, mt.TaskItemId
                              FROM aspnetusers u  
                              INNER JOIN MemberTasks mt on u.Id = mt.UserId 
                              WHERE mt.TaskItemId = @taskItemId;" +
                            // Get Comments
                            @"SELECT c.Id, c.Content, c.UpdateAt, u.FullName, u.Avatar, c.UserId, c.TaskItemId
                              FROM aspnetusers u  
                              INNER JOIN Comments c on u.Id = c.UserId
                              WHERE c.TaskItemId = @taskItemId;" +
                            // Get Subtasks
                            @"SELECT Id, Name, Status, TaskItemId
                              FROM Subtasks  
                              WHERE TaskItemId = @taskItemId;" +
                            // Get Labels
                            @"SELECT l.Id, Name, Color, WorkspaceId
                              FROM Labels l
                              INNER JOIN TaskLabels tl on l.Id = tl.LabelId
                              WHERE tl.TaskItemId = @taskItemId;";

                var parameters = new DynamicParameters();
                parameters.Add("taskItemId", taskItemId, DbType.Int32);

                TaskItemDto taskItemDto = null;
                using (var connection = _dapperContext.CreateConnection())
                using (var multiResult = await connection.QueryMultipleAsync(query, parameters))
                {
                    taskItemDto = await multiResult.ReadSingleOrDefaultAsync<TaskItemDto>();
                    if (taskItemDto != null)
                    {
                        taskItemDto.Members = (await multiResult.ReadAsync<MemberTaskDto>()).ToList();
                        if (taskItemDto.Members != null)
                        {
                            // Find member request extend due date
                            foreach (var member in taskItemDto.Members)
                            {
                                if (member.Requested)
                                    taskItemDto.MemberExtendDueDate = member;
                            }
                        }
                        taskItemDto.Comments = (await multiResult.ReadAsync<CommentDto>()).ToList();
                        taskItemDto.Subtasks = (await multiResult.ReadAsync<SubtaskDto>()).ToList();
                        taskItemDto.Labels = (await multiResult.ReadAsync<LabelDto>()).ToList();
                    }
                }

                // TaskItemDto taskItemDto =await _dapperContext.GetFirstAsync<TaskItemDto>(query, new {taskItemId});
                if (taskItemDto == null)
                {
                    return new Response
                    {
                        Message = "Nhiệm vụ không tồn tại",
                        IsSuccess = false
                    };
                }

                return new Response
                {
                    Message = "Lấy chi tiết nhiệm vụ thành công",
                    Data = new Dictionary<string, object>
                    {
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
                TaskItem taskItem = await _dapperContext.GetFirstAsync<TaskItem>(query, new { taskItemId });
                if (taskItem == null)
                {
                    return new Response
                    {
                        Message = "Nhiệm vụ không tồn tại",
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
                if (isUpdated)
                {
                    return new Response
                    {
                        Message = "Cập nhật nhiệm vụ thành công",
                        IsSuccess = true
                    };
                }
                return new Response
                {
                    Message = "Cập nhật nhiệm vụ thất bại",
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
            try
            {
                var mwAdmin = _dataContext.MemberWorkspaces.FirstOrDefault(
                    x => x.WorkspaceId == workspaceId &&
                    x.UserId == userId);
                if (mwAdmin.Role == ROLE_ENUM.Member)
                    return new Response
                    {
                        Message = "Bạn không được phép xóa nhiệm vụ",
                        IsSuccess = false
                    };

                var taskItem = _dataContext.TaskItems.FirstOrDefault(t => t.Id == taskItemId);

                var card = _dataContext.Cards.FirstOrDefault(c => c.Id == taskItem.CardId);
                if (card.TaskQuantity == 1)
                {
                    card.TaskOrder = "";
                    card.TaskQuantity = 0;
                }
                else
                {
                    card.TaskQuantity -= 1;
                    var check = card.TaskOrder.Contains($"{taskItemId},");
                    card.TaskOrder = check ? card.TaskOrder.Replace($"{taskItemId},", "") :
                                                card.TaskOrder.Replace($",{taskItemId}", "");
                }
                _dataContext.Cards.Update(card);

                _dataContext.TaskItems.Remove(taskItem);

                var workspace = _dataContext.Workspaces.FirstOrDefault(w => w.Id == workspaceId);
                workspace.TaskQuantity -= 1;
                workspace.VisitDate = DateTime.Now;

                if (card.Code == CARD_CODE_ENUM.Completed)
                {
                    workspace.TaskCompleted -= 1;
                }
                _dataContext.Workspaces.Update(workspace);

                var activation = new Activation
                {
                    UserId = userId,
                    WorkspaceId = workspaceId,
                    Content = $"Remove task {taskItem.Title}",
                    CreateAt = DateTime.Now
                };
                await _dataContext.Activations.AddAsync(activation);

                var isDeleted = await SaveChangeAsync();

                if (isDeleted)
                {
                    return new Response
                    {
                        Message = "Xóa nhiệm vụ thành công",
                        IsSuccess = true
                    };
                }
                return new Response
                {
                    Message = "Không thể xóa nhiệm vụ",
                    IsSuccess = false
                };
            }
            catch (Exception e)
            {
                Console.WriteLine("DeleteTaskItemAsync: " + e.Message);
                throw e;
            }
        }

        public async Task<bool> SaveChangeAsync()
        {
            return await _dataContext.SaveChangesAsync() > 0;
        }

        public async Task<Response> UploadFileAsync(int taskItemId, int workspaceId, string userId, IFormFile file)
        {
            try
            {
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
                var activation = new Activation
                {
                    UserId = userId,
                    WorkspaceId = workspaceId,
                    Content = $"Upload file {file.FileName} to task {taskItem.Title}",
                    CreateAt = DateTime.Now
                };
                await _dataContext.Activations.AddAsync(activation);

                var isUpdated = await SaveChangeAsync();

                if (isUpdated)
                {

                    return new Response
                    {
                        Message = "Tải tệp thành công",
                        Data = new Dictionary<string, object>
                        {
                            ["fileUrl"] = fileUrl
                        },
                        IsSuccess = true
                    };
                }
                return new Response
                {
                    Message = "Tải tệp thất bại",
                    IsSuccess = false
                };

            }
            catch (Exception e)
            {
                Console.WriteLine("UploadFileAsync: " + e.Message);
                throw e;
            }
        }

        public async Task<Response> PatchTaskItemAsync(int taskItemId, int workspaceId, string userId, JsonPatchDocument<TaskItem> patchTaskItem)
        {
            try
            {
                // Check user have permission to assign
                var mwAdmin = _dataContext.MemberWorkspaces.FirstOrDefault(
                    x => x.WorkspaceId == workspaceId &&
                    x.UserId == userId);
                if (mwAdmin.Role == ROLE_ENUM.Member)
                    return new Response
                    {
                        Message = "Bạn không được phép chỉnh sửa nhiệm vụ",
                        IsSuccess = false
                    };

                var taskItem = _dataContext.TaskItems.FirstOrDefault(c => c.Id == taskItemId);
                patchTaskItem.ApplyTo(taskItem);

                _dataContext.TaskItems.Update(taskItem);

                var activation = new Activation
                {
                    UserId = userId,
                    WorkspaceId = workspaceId,
                    Content = $"Chỉnh sửa nhiệm vụ {taskItem.Title}",
                    CreateAt = DateTime.Now
                };
                await _dataContext.Activations.AddAsync(activation);

                var isUpdated = await SaveChangeAsync();
                if (isUpdated)
                {

                    var activationDto = _mapper.Map<Activation, ActivationDto>(activation);
                    await _hubService.Clients.Group($"Workspace-{workspaceId}").SendActivationAsync(activationDto);
                    return new Response
                    {
                        Message = "Cập nhật nhiệm vụ thành công",
                        IsSuccess = true
                    };
                }
                return new Response
                {
                    Message = "Cập nhật nhiệm vụ thất bại",
                    IsSuccess = false
                };
            }
            catch (Exception e)
            {
                Console.WriteLine("PatchTaskItemAsync: " + e.Message);
                throw e;
            }
        }

        public async Task<Response> MoveTaskItemAsync(int taskItemId, int workspaceId, string userId, MoveTaskDto moveTaskDto)
        {
            try
            {
                var after = moveTaskDto.After;
                var before = moveTaskDto.Before;
                Card cardAfter = null;
                bool isUpdated = false;
                Activation activation = null;
                var taskItem = _dataContext.TaskItems.FirstOrDefault(t => t.Id == taskItemId);
                if (taskItem == null)
                    return new Response
                    {
                        Message = "Không tồn tại nhiệm vụ",
                        IsSuccess = false
                    };
                // If task move to another card
                if (after["cardId"] != before["cardId"])
                {
                    taskItem.CardId = after["cardId"];
                    _dataContext.TaskItems.Update(taskItem);

                    // Remove index of task item from card before
                    var cardBefore = _dataContext.Cards.FirstOrDefault(c => c.Id == before["cardId"]);
                    if (cardBefore == null)
                        return new Response
                        {
                            Message = "Di chuyển nhiệm vụ thất bại",
                            IsSuccess = false
                        };
                    if (cardBefore.TaskQuantity == 1)
                    {
                        cardBefore.TaskOrder = "";
                        cardBefore.TaskQuantity = 0;
                    }
                    else
                    {
                        var check = cardBefore.TaskOrder.Contains($"{taskItemId},");
                        cardBefore.TaskOrder = check ? cardBefore.TaskOrder.Replace($"{taskItemId},", "") :
                                                    cardBefore.TaskOrder.Replace($",{taskItemId}", "");
                        cardBefore.TaskQuantity -= 1;
                    }

                    // Add index of task item from card after                    
                    cardAfter = _dataContext.Cards.FirstOrDefault(c => c.Id == after["cardId"]);
                    if (cardAfter == null)
                        return new Response
                        {
                            Message = "Di chuyển nhiệm vụ thất bại",
                            IsSuccess = false
                        };
                    cardAfter.TaskOrder = InsertItemByIndex(cardAfter.TaskOrder, taskItemId, after["index"]);
                    cardAfter.TaskQuantity += 1;
                    _dataContext.Cards.UpdateRange(cardBefore, cardAfter);

                    if (cardAfter.Name == "Completed")
                    {
                        var workspace = _dataContext.Workspaces.FirstOrDefault(w => w.Id == workspaceId);
                        workspace.TaskCompleted += 1;
                        _dataContext.Workspaces.Update(workspace);
                    }
                    if (cardBefore.Name == "Completed")
                    {
                        var workspace = _dataContext.Workspaces.FirstOrDefault(w => w.Id == workspaceId);
                        workspace.TaskCompleted -= 1;
                        workspace.VisitDate = DateTime.Now;
                        _dataContext.Workspaces.Update(workspace);
                    }

                    activation = new Activation
                    {
                        UserId = userId,
                        WorkspaceId = workspaceId,
                        Content = $"Di chuyển {taskItem.Title} từ thẻ {cardBefore.Name} tới thẻ {cardAfter.Name}",
                        CreateAt = DateTime.Now
                    };
                    await _dataContext.Activations.AddAsync(activation);

                    isUpdated = await SaveChangeAsync();
                    if (isUpdated)
                    {
                        return new Response
                        {
                            Message = "Di chuyển nhiệm vụ thành công",
                            Data = new Dictionary<string, object>
                            {
                                ["taskItem"] = _mapper.Map<TaskItem, TaskItemDto>(taskItem),
                            },
                            IsSuccess = true
                        };
                    }
                    return new Response
                    {
                        Message = "Di chuyển nhiệm vụ thất bại",
                        IsSuccess = false
                    };
                }
                else
                {

                    cardAfter = _dataContext.Cards.FirstOrDefault(c => c.Id == after["cardId"]);
                    cardAfter.TaskOrder = InsertItemByIndex(cardAfter.TaskOrder, taskItemId, after["index"], before["index"]);

                    _dataContext.Cards.Update(cardAfter);

                    activation = new Activation
                    {
                        UserId = userId,
                        WorkspaceId = workspaceId,
                        Content = $"Di chuyển nhiệm vụ {taskItem.Title} trong thẻ {cardAfter.Name}",
                        CreateAt = DateTime.Now
                    };
                    await _dataContext.Activations.AddAsync(activation);

                    isUpdated = await SaveChangeAsync();
                    if (isUpdated)
                    {
                        return new Response
                        {
                            Message = "Di chuyển nhiệm vụ thành công",
                            Data = new Dictionary<string, object>
                            {
                                ["taskItem"] = _mapper.Map<TaskItem, TaskItemDto>(taskItem),
                            },
                            IsSuccess = true
                        };
                    }
                    return new Response
                    {
                        Message = "Di chuyển nhiệm vụ thất bại",
                        IsSuccess = false
                    };
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("MoveTaskItemAsync: " + e.Message);
                throw e;
            }
        }

        private string InsertItemByIndex(string indexes, int item, int newIndex, int oldIndex = -1)
        {
            List<int> listIndex = null;
            if (indexes == "")
            {
                listIndex = new List<int>();
            }
            else
            {
                listIndex = JsonConvert.DeserializeObject<List<int>>(indexes);
            }
            if (oldIndex != -1)
                listIndex.RemoveAt(oldIndex);

            listIndex.Insert(newIndex, item);
            string rs = JsonConvert.SerializeObject(listIndex);
            return rs;
        }

        #region Member is assigned in task item
        public async Task<Response> GetTasksItemByMemberAsync(string memberId, PRIORITY_ENUM? priority, bool? isComplete, bool? desc)
        {
            try
            {
                var query = @"SELECT u.Id, FullName, Avatar, Email, mw.Role
                              FROM aspnetusers u
                              INNER JOIN MemberWorkspaces mw on mw.UserId = u.Id
                              WHERE u.Id = @memberId";
                var parameters = new DynamicParameters();
                parameters.Add("memberId", memberId, DbType.String);

                var member = await _dapperContext.GetFirstAsync<MemberWorkspaceDto>(query, parameters);
                if (member == null)
                    return new Response
                    {
                        Message = "Thành viên không tồn tại",
                        IsSuccess = false
                    };

                query = @"SELECT t.Id, Title, Priority, DueDate, IsComplete, SubtaskQuantity, SubtaskCompleted,
                                 u.Id as CreatorId, u.FullName, u.Avatar, u.Email
                          FROM TaskItems t 
                          INNER JOIN aspnetusers u on u.Id = t.CreatorId
                          INNER JOIN MemberTasks mt on mt.TaskItemId = t.Id
                          WHERE mt.UserId = @memberId";
                // Filter by priority and isComplete
                if (priority != null)
                {
                    query += " AND t.Priority = @priority";
                    parameters.Add("priority", priority, DbType.Int32);
                }
                if (isComplete != null)
                {
                    query += " AND t.IsComplete = @isComplete";
                    parameters.Add("isComplete", isComplete, DbType.Boolean);
                }


                List<TaskItemDto> taskItemDtos = await _dapperContext.GetListAsync<TaskItemDto>(query, parameters);
                if (taskItemDtos == null)
                    return new Response
                    {
                        Message = "Thành viên chưa được gán task",
                        IsSuccess = false
                    };

                // Sorting 
                if (desc != null)
                {
                    if (desc == true)
                        taskItemDtos = taskItemDtos.OrderByDescending(x => x.DueDate).ToList();
                    else
                        taskItemDtos = taskItemDtos.OrderBy(x => x.DueDate).ToList();
                }

                return new Response
                {
                    Message = "Lấy danh sách nhiệm vụ thành công",
                    Data = new Dictionary<string, object>
                    {
                        ["Member"] = member,
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
            try
            {
                // Check user have permission to assign
                var creator = _dataContext.TaskItems.FirstOrDefault(
                    x => x.Id == taskItemId &&
                    x.CreatorId == userId);
                if (creator == null)
                    return new Response
                    {
                        Message = "Bạn không được phép gán thành viên",
                        IsSuccess = false
                    };

                var membersNew = _mapper.Map<List<MemberTaskDto>, List<MemberTask>>(memberTaskDtos);
                List<MemberTask> membersOld = _dataContext.MemberTasks.Where(x => x.TaskItemId == taskItemId).ToList();
                // Check member new has been members old
                foreach (var memberTask in membersNew)
                {
                    if (membersOld.Contains(memberTask))
                        membersOld.Remove(memberTask);
                    else
                        _dataContext.MemberTasks.Add(memberTask);
                }
                // Remove the old members
                foreach (var memberTask in membersOld)
                {
                    _dataContext.MemberTasks.Remove(memberTask);
                }

                var isSaved = await SaveChangeAsync();

                var members = _dataContext.MemberTasks.Where(m => m.TaskItemId == taskItemId).ToList();
                return new Response
                {
                    Message = "Gán thành viên vào nhiệm vụ thành công",
                    Data = new Dictionary<string, object>
                    {
                        ["Members"] = _mapper.Map<List<MemberTask>, List<MemberTaskDto>>(members),
                    },
                    IsSuccess = true
                };

            }
            catch (Exception e)
            {
                Console.WriteLine("AssignMemberAsync: " + e.Message);
                throw e;
            }
        }

        public async Task<Response> ExtendDueDateByMemberAsync(int workspaceId, string userId, MemberTaskDto memberTaskDto)
        {
            try
            {
                var memberTask = _dataContext.MemberTasks.FirstOrDefault(
                    m => m.TaskItemId == memberTaskDto.TaskItemId
                    && m.UserId == userId);
                if (memberTask == null)
                    return new Response
                    {
                        Message = "Bạn chưa được gán vào nhiệm vụ này",
                        IsSuccess = false
                    };

                memberTask.ExtendDate = memberTaskDto.ExtendDate;
                memberTask.Requested = true;

                _dataContext.MemberTasks.Update(memberTask);

                var taskItem = _dataContext.TaskItems.FirstOrDefault(t => t.Id == memberTaskDto.TaskItemId);

                var activation = new Activation
                {
                    UserId = userId,
                    WorkspaceId = workspaceId,
                    Content = $"Thành viên yêu cầu gia hạn vào {memberTask.ExtendDate.Value.ToShortDateString()} trong nhiệm vụ {taskItem.Title}",
                    CreateAt = DateTime.Now
                };
                _dataContext.Activations.Add(activation);

                var isSaved = await SaveChangeAsync();

                if (isSaved)
                {
                    var activationDto = _mapper.Map<Activation, ActivationDto>(activation);
                    await _hubService.Clients.Group($"Workspace-{workspaceId}").SendActivationAsync(activationDto);
                    return new Response
                    {
                        Message = "Yêu cầu gia hạn thành công",
                        IsSuccess = true
                    };
                }
                return new Response
                {
                    Message = "Yêu cầu gia hạn thất bại",
                    IsSuccess = false
                };
            }
            catch (Exception e)
            {
                Console.WriteLine("CreateCommentAsync: " + e.Message);
                throw e;
            }
        }

        public async Task<Response> AcceptExtendDueDateAsync(int workspaceId, string userId, MemberTaskDto memberTaskDto)
        {
            try
            {
                

                var memberTask = _dataContext.MemberTasks.FirstOrDefault(
                    m => m.TaskItemId == memberTaskDto.TaskItemId
                    && m.UserId == memberTaskDto.UserId);

                if (memberTask == null)
                    return new Response
                    {
                        Message = "Bạn không được phép thực hiện chức năng này",
                        IsSuccess = false
                    };

                // Check user have permission to assign
                var creatorTask = _dataContext.TaskItems.FirstOrDefault(
                    x => x.Id == memberTask.TaskItemId &&
                    x.CreatorId == userId);
                if (creatorTask == null)
                    return new Response
                    {
                        Message = "Bạn không được phép gán thành viên",
                        IsSuccess = false
                    };

                memberTask.ExtendDate = null;
                memberTask.Requested = false;

                _dataContext.MemberTasks.Update(memberTask);

                creatorTask.DueDate = memberTaskDto.ExtendDate;
                _dataContext.TaskItems.Update(creatorTask);

                var activation = new Activation
                {
                    UserId = userId,
                    WorkspaceId = workspaceId,
                    Content = $"Chấp nhận yêu cầu gia hạn vào {memberTaskDto.ExtendDate.Value.ToString("dd/MM/yyyy H:mm:ss")} trong nhiệm vụ {creatorTask.Title}",
                    CreateAt = DateTime.Now
                };
                _dataContext.Activations.Add(activation);

                var isSaved = await SaveChangeAsync();

                if (isSaved)
                {
                    var activationDto = _mapper.Map<Activation, ActivationDto>(activation);
                    await _hubService.Clients.Group($"Workspace-{workspaceId}").SendActivationAsync(activationDto);
                    return new Response
                    {
                        Message = "Chấp nhận yêu cầu gia hạn thành công",
                        IsSuccess = true
                    };
                }
                return new Response
                {
                    Message = "Chấp nhận yêu cầu gia hạn thất bại",
                    IsSuccess = false
                };
            }
            catch (Exception e)
            {
                Console.WriteLine("AcceptExtendDueDateAsync: " + e.Message);
                throw e;
            }
        }
        public async Task<Response> RejectExtendDueDateAsync(int workspaceId, string userId, int memberTaskId)
        {
            try
            {
                var memberTask = _dataContext.MemberTasks.FirstOrDefault(
                    m => m.Id == memberTaskId);
                if (memberTask == null)
                    return new Response
                    {
                        Message = "Bạn không được phép thực hiện chức năng này",
                        IsSuccess = false
                    };

                // Check user have permission to assign
                var creatorTask = _dataContext.TaskItems.FirstOrDefault(
                    x => x.Id == memberTask.TaskItemId &&
                    x.CreatorId == userId);

                if (creatorTask == null)
                    return new Response
                    {
                        Message = "Bạn không được phép gán thành viên",
                        IsSuccess = false
                    };

                
                memberTask.ExtendDate = null;
                memberTask.Requested = false;
                _dataContext.MemberTasks.Update(memberTask);

                var isSaved = await SaveChangeAsync();

                if (isSaved)
                {
                    return new Response
                    {
                        Message = "Chấp nhận yêu cầu gia hạn thành công",
                        IsSuccess = true
                    };
                }
                return new Response
                {
                    Message = "Chấp nhận yêu cầu gia hạn thất bại",
                    IsSuccess = false
                };
            }
            catch (Exception e)
            {
                Console.WriteLine("CreateCommentAsync: " + e.Message);
                throw e;
            }
        }

        #endregion

        #region User comment in task item
        public async Task<Response> CreateCommentAsync(int workspaceId, string userId, CommentDto commentDto)
        {
            try
            {
                var taskItem = _dataContext.TaskItems.FirstOrDefault(t => t.Id == commentDto.TaskItemId);
                if (taskItem == null)
                    return new Response
                    {
                        Message = "Không tìm thầy nhiệm vụ",
                        IsSuccess = false
                    };

                var comment = _mapper.Map<CommentDto, Comment>(commentDto);
                comment.UserId = userId;
                comment.UpdateAt = DateTime.Now;
                _dataContext.Comments.Add(comment);

                taskItem.CommentQuantity +=1;
                _dataContext.TaskItems.Update(taskItem);

                var activation = new Activation
                {
                    UserId = userId,
                    WorkspaceId = workspaceId,
                    Content = $"Thành viên bình luận ở trong nhiệm vụ {taskItem.Title}",
                    CreateAt = DateTime.Now
                };
                _dataContext.Activations.Add(activation);

                var isSaved = await SaveChangeAsync();

                if (isSaved)
                {
                    var activationDto = _mapper.Map<Activation, ActivationDto>(activation);
                    await _hubService.Clients.Group($"Workspace-{workspaceId}").SendActivationAsync(activationDto);

                    commentDto = _mapper.Map<Comment, CommentDto>(comment);
                    return new Response
                    {
                        Message = "Tạo bình luận thành công",
                        Data = new Dictionary<string, object>
                        {
                            ["Comment"] = commentDto,
                        },
                        IsSuccess = true
                    };
                }
                return new Response
                {
                    Message = "Tạo bình luận thất bại",
                    IsSuccess = false
                };
            }
            catch (Exception e)
            {
                Console.WriteLine("CreateCommentAsync: " + e.Message);
                throw e;
            }
        }

        public async Task<Response> EditCommentAsync(int commentId, string userId, CommentDto commentDto)
        {
            try
            {
                if (userId != commentDto.UserId)
                    return new Response
                    {
                        Message = "Bạn không được chỉnh sửa bình luận của người khác",
                        IsSuccess = false
                    };

                var comment = _dataContext.Comments.FirstOrDefault(t => t.Id == commentId);
                if (comment == null)
                    return new Response
                    {
                        Message = "Không tìm thấy bình luận",
                        IsSuccess = false
                    };

                comment.Content = commentDto.Content;
                comment.UpdateAt = DateTime.Now;
                _dataContext.Comments.Update(comment);

                var isSaved = await SaveChangeAsync();

                if (isSaved)
                {
                    commentDto = _mapper.Map<Comment, CommentDto>(comment);
                    return new Response
                    {
                        Message = "Chỉnh sửa bình luận thành công",
                        Data = new Dictionary<string, object>
                        {
                            ["Comment"] = commentDto,
                        },
                        IsSuccess = true
                    };
                }
                return new Response
                {
                    Message = "Chỉnh sửa bình luận thất bại",
                    IsSuccess = false
                };
            }
            catch (Exception e)
            {
                Console.WriteLine("EditCommentAsync: " + e.Message);
                throw e;
            }
        }

        public async Task<Response> DeleteCommentAsync(int commentId, string userId)
        {
            try
            {
                var comment = _dataContext.Comments.FirstOrDefault(t => t.Id == commentId && t.UserId == userId);

                if (comment == null)
                    return new Response
                    {
                        Message = "Không tìm thấy bình luận",
                        IsSuccess = false
                    };

                var taskItem = _dataContext.TaskItems.FirstOrDefault(t => t.Id == comment.TaskItemId);
                taskItem.CommentQuantity -= 1;
                _dataContext.TaskItems.Update(taskItem);
                _dataContext.Comments.Remove(comment);

                

                var isSaved = await SaveChangeAsync();

                if (isSaved)
                {
                    return new Response
                    {
                        Message = "Xóa bình luận thành công",
                        IsSuccess = true
                    };
                }
                return new Response
                {
                    Message = "Xóa bình luận thất bại",
                    IsSuccess = false
                };
            }
            catch (Exception e)
            {
                Console.WriteLine("DeleteCommentAsync: " + e.Message);
                throw e;
            }
        }

        #endregion
        public async Task<Response> AddLabelToTaskItemAsync(int taskItemId, List<LabelDto> labelDtos)
        {
            try
            {
                var taskItem = _dataContext.TaskItems.FirstOrDefault(t => t.Id == taskItemId);
                if (taskItem == null)
                    return new Response
                    {
                        Message = "Không tìm thấy nhiệm vụ",
                        IsSuccess = false
                    };

                List<TaskLabel> taskLabelsOld = _dataContext.TaskLabels.Where(x => x.TaskItemId == taskItemId).ToList();
                // Check member new has been members old
                foreach (var labelDto in labelDtos)
                {
                    var taskLabel = taskLabelsOld.Find(x => x.TaskItemId == taskItemId && x.LabelId == labelDto.Id);
                    if (taskLabel == null)
                    {
                        taskLabel = new TaskLabel
                        {
                            TaskItemId = taskItemId,
                            LabelId = labelDto.Id
                        };
                        _dataContext.TaskLabels.Add(taskLabel);
                    }
                    else
                        taskLabelsOld.Remove(taskLabel);

                }
                // Remove the old members
                foreach (var taskLabel in taskLabelsOld)
                {
                    _dataContext.TaskLabels.Remove(taskLabel);
                }

                var isSaved = await SaveChangeAsync();
                return new Response
                {
                    Message = "Lấy danh sách nhãn thành công",
                    Data = new Dictionary<string, object>
                    {
                        ["Labels"] = labelDtos,
                    },
                    IsSuccess = true
                };
            }
            catch (Exception e)
            {
                Console.WriteLine("CreateCommentAsync: " + e.Message);
                throw e;
            }
        }


    }
}