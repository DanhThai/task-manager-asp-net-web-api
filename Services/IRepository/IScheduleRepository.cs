using TaskManager.API.Data.DTOs;

namespace TaskManager.API.Services.IRepository
{
    public interface IScheduleRepository
    {
        public Task<Response> GetScheduleByIdAsync(int scheduleId);
        public Task<Response> CreateScheduleAsync(string userId, ScheduleDto scheduleDto);
        public Task<Response> UpdateScheduleAsync(int scheduleId, string userId, ScheduleDto scheduleDto);
        // public Task<Response> PatchScheduleAsync(int scheduleId, int WorkspaceId, string userId, JsonPatchDocument<Schedule> patchSchedule);
        public Task<Response> DeleteScheduleAsync(int scheduleId, string userId);
        public Task<bool> SaveChangeAsync();
    }
}