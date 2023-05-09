

using Microsoft.AspNetCore.JsonPatch;
using TaskManager.API.Data.DTOs;
using TaskManager.API.Data.Models;

namespace TaskManager.API.Services.IRepository
{
    public interface ITaskItemRepository
    {
        public Task<Response> GetTaskItemByIdAsync(int taskItemId);
        public Task<Response> CreateTaskItemAsync(TaskItemDto taskItemDto, string userId);
        public Task<Response> UpdateTaskItemAsync(TaskItemDto taskItemDto, int taskItemId);
        public Task<Response> PatchTaskItemAsync(JsonPatchDocument<TaskItem> patchTaskItem, int taskItemId);
        public Task<Response> UploadFileAsync(int taskItemId, IFormFile file);
        public Task<Response> DeleteTaskItemAsync(int taskItemId);
        public Task<bool> SaveChangeAsync();
    }
}