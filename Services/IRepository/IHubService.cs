using TaskManager.API.Data.DTOs;

namespace TaskManager.API.Services.IRepository
{
    public interface IHubService
    {
        public Task SendActivationAsync(ActivationDto activationDto);
        public Task SendWorkspaceAsync(WorkspaceDto workspaceDto);
        public Task SendCardAsync(CardDto cardDto);
    }
}