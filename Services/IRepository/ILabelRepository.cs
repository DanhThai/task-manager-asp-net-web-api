using TaskManager.API.Data.DTOs;

namespace TaskManager.API.Services.IRepository
{
    public interface ILabelRepository
    {
        public Task<Response> GetListLabelByWorkspaceIdAsync(int workspaceId);
        public Task<Response> CreateLabelAsync(LabelDto LabelDto);
        public Task<Response> UpdateLabelAsync(int labelId, LabelDto LabelDto);
        public Task<Response> DeleteLabelAsync(int labelId);
        public Task<bool> SaveChangeAsync();
    }
}