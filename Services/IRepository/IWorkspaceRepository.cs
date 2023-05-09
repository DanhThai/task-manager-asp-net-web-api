
using TaskManager.API.Data.DTOs;

namespace TaskManager.API.Services.IRepository
{
    public interface IWorkspaceRepository
    {
        public Task<Response> GetWorkspacesByUserAsync(string userId);
        public Task<Response> GetWorkspaceByIdAsync(int workspaceId, string userId);

        public Task<Response> CreateWorkspaceAsync(WorkspaceDto workspaceDto, string userId, string userName);
        public Task<Response> UpdateWorkspaceAsync(WorkspaceDto workspaceDto);
        public Task<Response> DeleteWorkspaceAsync(int workspaceId);
        public Task<bool> SaveChangeAsync();
   
    }
}