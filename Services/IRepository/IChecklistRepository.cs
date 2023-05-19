using TaskManager.API.Data.DTOs;

namespace TaskManager.API.Services.IRepository
{
    public interface IChecklistRepository
    {
        public Task<Response> GetChecklistByIdAsync(int checklistId);
        public Task<Response> CreateChecklistAsync(int workspaceId, string userId, ChecklistDto checklistDto);
        public Task<Response> UpdateChecklistAsync(int checklistId, int workspaceId, string userId, ChecklistDto checklistDto);
        public Task<Response> DeleteChecklistAsync(int checklistId, int workspaceId, string userId);
        public Task<bool> SaveChangeAsync();
    }
}