

using Microsoft.AspNetCore.JsonPatch;
using TaskManager.API.Data.DTOs;
using TaskManager.API.Data.Models;

namespace TaskManager.API.Services.IRepository
{
    public interface ITaskItemRepository
    {
        public Task<Response> GetTaskItemByIdAsync(int taskItemId);
        public Task<Response> CreateTaskItemAsync(int WorkspaceId, string userId, TaskItemDto taskItemDto);
        public Task<Response> UpdateTaskItemAsync(int taskItemId, int WorkspaceId, string userId, TaskItemDto taskItemDto);
        public Task<Response> MoveTaskItemAsync(int taskItemId, int WorkspaceId, string userId, MoveTaskDto moveTaskDto);
        public Task<Response> PatchTaskItemAsync(int taskItemId, int WorkspaceId, string userId, JsonPatchDocument<TaskItem> patchTaskItem);
        public Task<Response> UploadFileAsync(int taskItemId, int WorkspaceId, string userId, IFormFile file);
        public Task<Response> DeleteTaskItemAsync(int taskItemId, int WorkspaceId, string userId);
        public Task<bool> SaveChangeAsync();
    }
}