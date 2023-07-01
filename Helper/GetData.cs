
using System.Data;
using Dapper;
using Newtonsoft.Json;
using TaskManager.API.Data;
using TaskManager.API.Data.DTOs;

namespace TaskManager.API.Helper
{
    public class GetData
    {
        private readonly DapperContext _dapperContext;

        public GetData(DapperContext dapperContext)
        {
            _dapperContext = dapperContext;
        }

        public async Task<Response> GetWorkspaceById(int workspaceId, string userId)
        {
            var currentDay = DateTime.Now.AddDays(-10);
            var query = @"SELECT Id, Title, Description, Permission, CreatorId, CreatorName, TaskQuantity, TaskCompleted 
                              FROM Workspaces w WHERE w.Id = @WorkspaceId;" +
                        @"SELECT mw.UserId, u.FullName, u.Email, u.Avatar, mw.Role
                              FROM aspnetusers u
                              INNER JOIN MemberWorkspaces mw on u.Id = mw.UserId 
                              WHERE mw.WorkspaceId = @WorkspaceId;" +
                        @"SELECT Id, Name, Code, TaskQuantity, TaskOrder
                              FROM Cards
                              WHERE WorkspaceId = @WorkspaceId;" +
                        @"SELECT Content, CreateAt, UserId, Avatar, FullName
                              FROM Activations a
                              INNER JOIN aspnetusers u on u.Id = a.UserId 
                              WHERE WorkspaceId = @WorkspaceId
                              ORDER BY a.CreateAt DESC LIMIT 10;";

            var parameters = new DynamicParameters();
            parameters.Add("WorkspaceId", workspaceId, DbType.Int32);
            parameters.Add("currentDay", currentDay, DbType.DateTime);

            WorkspaceDto workspaceDto = null;
            using (var connection = _dapperContext.CreateConnection())
            using (var multiResult = await connection.QueryMultipleAsync(query, parameters))
            {
                workspaceDto = await multiResult.ReadSingleOrDefaultAsync<WorkspaceDto>();
                if (workspaceDto != null)
                {
                    workspaceDto.Members = (await multiResult.ReadAsync<MemberWorkspaceDto>()).ToList();
                    if (workspaceDto.Members != null)
                    {
                        // Check member has been workspace
                        foreach (var member in workspaceDto.Members)
                        {
                            if (member.UserId == userId)
                            {
                                workspaceDto.MyRole = (int)member.Role;
                                break;
                            }
                        }
                    }

                    workspaceDto.Cards = (await multiResult.ReadAsync<CardDto>()).ToList();
                    foreach (var card in workspaceDto.Cards)
                    {
                        var listTaskItem = card.TaskOrder.ConvertStringToList();
                        card.ListTaskIdOrder = listTaskItem;
                        if (listTaskItem != null)
                            for (int i = 0; i < listTaskItem.Count; i++)
                            {
                                card.TaskItems.Add(null);
                            }
                    }
                    workspaceDto.Activations = (await multiResult.ReadAsync<ActivationDto>()).ToList();
                }
            }
            if (workspaceDto == null)
            {
                return new Response
                {
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
                var multiResult = await connection.QueryAsync<TaskItemDto, MemberTaskDto, LabelDto, TaskItemDto>(
                query, (taskItem, memberTask, label) =>
                {
                    if (!taskItemDict.TryGetValue(taskItem.Id, out var currenttaskItem))
                    {
                        currenttaskItem = taskItem;
                        taskItemDict.Add(taskItem.Id, currenttaskItem);
                    }
                    if (memberTask != null && currenttaskItem.Members.FirstOrDefault(m => m.UserId == memberTask.UserId) == null)
                    {
                        currenttaskItem.Members.Add(memberTask);
                    }
                    if (label != null
                        && currenttaskItem.Labels.FirstOrDefault(m => m.Id == label.Id) == null)
                    {
                        currenttaskItem.Labels.Add(label);
                    }
                    return currenttaskItem;
                },
                parameters
                , splitOn: "UserId, Id");
            }

            // Add task item to card
            foreach (var taskItem in taskItemDict.Values)
            {
                foreach (var card in workspaceDto.Cards)
                {
                    if (card.TaskQuantity > 0)
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
            return new Response
            {
                Message = "Lấy dự án thành công",
                Data = new Dictionary<string, object>
                {
                    ["Workspace"] = workspaceDto
                },
                IsSuccess = true
            };
        }

        public async Task<Response> GetTaskItemById(int taskItemId)
        {
            try
            {
                var query = @"SELECT t.Id, Title, Description, Attachment, Priority, DueDate, CardId, IsComplete, SubtaskQuantity, SubtaskCompleted,
                                     u.Id as CreatorId, u.FullName, u.Avatar, u.Email
                              FROM TaskItems t 
                              INNER JOIN aspnetusers u on u.Id = t.CreatorId
                              WHERE t.Id = @taskItemId;" +
                            // Get member is assigned
                            @"SELECT mt.Id, Requested, ExtendDate, u.Id as UserId, u.FullName, u.Avatar, u.Email, mt.TaskItemId
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
    }
}