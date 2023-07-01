using Microsoft.AspNetCore.SignalR;
using TaskManager.API.Data.DTOs;
using TaskManager.API.Services.IRepository;

namespace TaskManager.API.Services.Repository
{
    public class HubService: Hub<IHubService>
    {
        // public async Task SendActivationAsync(ActivationDto activationDto){
        //     await Clients.OthersInGroup($"workspace-{activationDto.WorkspaceId}").ActivationAsync(activationDto);
        // }

        // public async Task SendWorkspaceAsync(Response workspaceDto){
        //     await Clients.OthersInGroup($"workspace-{workspaceDto.Id}").WorkspaceAsync(workspaceDto);
        // }

        public async Task ConnectToWorkspace(int workspaceId){
            await Groups.AddToGroupAsync(Context.ConnectionId, $"workspace-{workspaceId}");
            await Clients.Group($"workspace-{workspaceId}").SendMessageAsync($"Bạn {Context.ConnectionId} đã tham gia dự án {workspaceId}");
        }

        public async Task DisconnectToWorkspace(int workspaceId){
            await Clients.Group($"workspace-{workspaceId}").SendMessageAsync($"Bạn {Context.ConnectionId} đã rời dự án {workspaceId}");
            
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"workspace-{workspaceId}");

        }

         public async Task ConnectToTaskItem(int taskItemId){
            await Groups.AddToGroupAsync(Context.ConnectionId, $"taskItem-{taskItemId}");
            await Clients.Group($"taskItem-{taskItemId}").SendMessageAsync($"Bạn {Context.ConnectionId} đã tham gia nhiệm vụ {taskItemId}");
        }

        public async Task DisconnectToTaskItem(int taskItemId){
            await Clients.Group($"taskItem-{taskItemId}").SendMessageAsync($"Bạn {Context.ConnectionId} đã rời nhiệm vụ");

            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"taskItem-{taskItemId}");
        }
        
    }
}