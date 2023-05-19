using Microsoft.AspNetCore.SignalR;
using TaskManager.API.Data.DTOs;
using TaskManager.API.Services.IRepository;

namespace TaskManager.API.Services.Repository
{
    public class HubService: Hub<IHubService>
    {
        public async Task SendActivationAsync(ActivationDto activationDto){
            await Clients.OthersInGroup($"Workspace-{activationDto.WorkspaceId}").SendActivationAsync(activationDto);
        }

        public async Task SendWorkspaceAsync(WorkspaceDto workspaceDto){
            await Clients.OthersInGroup($"Workspace-{workspaceDto.Id}").SendWorkspaceAsync(workspaceDto);
        }

        public async Task ConnectToWorkspaceAsync(int workspaceId){
            await Groups.AddToGroupAsync(Context.ConnectionId, $"workspace-{workspaceId}");
        }

        public async Task DisconnectToWorkspaceAsync(int workspaceId){
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"workspace-{workspaceId}");
        }
        
    }
}