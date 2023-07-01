using Microsoft.AspNetCore.JsonPatch;
using TaskManager.API.Data.DTOs;
using TaskManager.API.Data.Models;

namespace TaskManager.API.Services.IRepository
{
    public interface IScheduleRepository
    {
        public Task<Response> GetScheduleByIdAsync(int scheduleId);
        public Task<Response> GetSchedulesByWorkspaceAsync(int workspaceId);

        public Task<Response> CreateScheduleAsync(string userId, ScheduleDto scheduleDto);
        public Task<Response> UpdateScheduleAsync(int scheduleId, string userId, ScheduleDto scheduleDto);
        public Task<Response> PatchScheduleAsync(int scheduleId, int workspaceId, string userId, JsonPatchDocument<Schedule> patchSchedule);
        public Task<Response> DeleteScheduleAsync(int scheduleId, int workspaceId, string userId);
        public Task<bool> SaveChangeAsync();
    }
}