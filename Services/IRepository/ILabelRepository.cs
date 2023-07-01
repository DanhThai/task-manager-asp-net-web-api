using TaskManager.API.Data.DTOs;

namespace TaskManager.API.Services.IRepository
{
    public interface ILabelRepository
    {
        public Task<Response> GetListLabelByWorkspaceIdAsync(int workspaceId);
        public Task<Response> CreateLabelAsync(LabelDto LabelDto, string userId);
        public Task<Response> UpdateLabelAsync(int labelId, string userId, LabelDto LabelDto);
        public Task<Response> DeleteLabelAsync(int labelId, string userId);
        public Task<bool> SaveChangeAsync();
    }
}