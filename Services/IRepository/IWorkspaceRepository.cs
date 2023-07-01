
using TaskManager.API.Data.DTOs;

namespace TaskManager.API.Services.IRepository
{
    public interface IWorkspaceRepository
    {
        public Task<Response> GetWorkspacesByUserAsync(string userId);
        public Task<Response> GetWorkspaceRecentlyAsync(string userId);
        public Task<Response> GetWorkspaceByIdAsync(int workspaceId, string userId);
        public Task<Response> CreateWorkspaceAsync(WorkspaceDto workspaceDto, string userId, string userName);
        public Task<Response> UpdateWorkspaceAsync(WorkspaceDto workspaceDto, string userId);
        public Task<Response> DeleteWorkspaceAsync(int workspaceId, string userId);

        public Task<Response> GetCardsOfWorkspaceAsync(int workspaceId);


        public Task<Response> InviteMemberToWorkspaceAsync(int workspaceId, string userId, MemberWorkspaceDto member);
        public Task<Response> ConfirmMemberWorkspaceAsync(int workspaceId, string userId, int role);
        public Task<Response> LeaveOnWorkspaceAsync(int workspaceId, string userId);
        public Task<Response> RemoveMemberToWorkspaceAsync(int workspaceId, string userId, string memberId);

        public Task<Response> GetMembersOfWorkspaceAsync(int workspaceId);
        public Task<Response> GetMembersWithTaskItemAsync(int workspaceId, string userId);
        
        public Task<bool> SaveChangeAsync();
   
    }
}