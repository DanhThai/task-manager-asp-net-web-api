
using Microsoft.AspNetCore.JsonPatch;
using TaskManager.API.Data.DTOs;
using TaskManager.API.Data.Models;

namespace TaskManager.API.Services.IRepository
{
    public interface ISubtaskRepository
    {
        public Task<Response> GetSubtaskByIdAsync(int subtaskId);
        public Task<Response> CreateSubtaskAsync(int workspaceId, string userId, SubtaskDto subtaskDto);
        public Task<Response> UpdateSubtaskAsync(int subtaskId, int workspaceId, string userId, SubtaskDto subtaskDto);
        public Task<Response> PatchSubtaskAsync(int subtaskId, int WorkspaceId, string userId, JsonPatchDocument<Subtask> patchSubtask);

        public Task<Response> DeleteSubtaskAsync(int subtaskId, int workspaceId, string userId);
        public Task<bool> SaveChangeAsync();
    }
}