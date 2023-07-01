using TaskManager.API.Data.DTOs;

namespace TaskManager.API.Services.IRepository
{
    public interface IHubService
    {
        public Task ConnectToWorkspaceAsync(string workspaceId);
        public  Task SendMessageAsync(string message);

        public Task ActivationAsync(ActivationDto activationDto);
        public Task WorkspaceAsync(Response ResponseWorkspaceDto);
        public Task TaskItemAsync(Response resTaskItemDto);
    }
}