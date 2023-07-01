

using Microsoft.AspNetCore.JsonPatch;
using TaskManager.API.Data.DTOs;
using TaskManager.API.Data.Models;

namespace TaskManager.API.Services.IRepository
{
    public interface ITaskItemRepository
    {
        public Task<Response> GetTaskItemByIdAsync(int taskItemId);
        public Task<Response> GetUpCommingTasksItemAsync(string userId);

        public Task<Response> CreateTaskItemAsync(int WorkspaceId, string userId, TaskItemDto taskItemDto);
        public Task<Response> UpdateTaskItemAsync(int taskItemId, int WorkspaceId, string userId, TaskItemDto taskItemDto);
        public Task<Response> MoveTaskItemAsync(int taskItemId, int WorkspaceId, string userId, MoveTaskDto moveTaskDto);
        public Task<Response> PatchTaskItemAsync(int taskItemId, int WorkspaceId, string userId, JsonPatchDocument<TaskItem> patchTaskItem);
        public Task<Response> UploadFileAsync(int taskItemId, int WorkspaceId, string userId, IFormFile file);
        public Task<Response> DeleteTaskItemAsync(int taskItemId, int WorkspaceId, string userId);

        public Task<Response> GetTasksItemByMemberAsync(string memberId, int workspaceId, PRIORITY_ENUM? priority, bool? isComplete, bool? desc);
        public Task<Response> AssignMemberAsync(int taskItemId, int workspaceId, string userId, List<MemberTaskDto> memberTaskDto);
        public Task<Response> ExtendDueDateByMemberAsync(int workspaceId, string userId, MemberTaskDto memberTaskDto);
        public Task<Response> AcceptExtendDueDateAsync(int workspaceId, string userId, int memberTaskId);
        public Task<Response> RejectExtendDueDateAsync(int workspaceId, string userId, int memberTaskId);
        // public Task<Response> RemoveMemberAsync(int workspaceId, string userId, MemberTaskDto memberTaskDto);

        public Task<Response> CreateCommentAsync(int workspaceId, string userId, CommentDto commentDto);
        public Task<Response> EditCommentAsync(int commentId, string userId, CommentDto commentDto);
        public Task<Response> DeleteCommentAsync(int commentId, string userId );

        public Task<Response> AddLabelToTaskItemAsync(int taskItemId, int workspaceId, string userId, List<LabelDto> labelDto);


        public Task<bool> SaveChangeAsync();
    }
}