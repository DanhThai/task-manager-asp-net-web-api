
using TaskManager.API.Data.DTOs;

namespace TaskManager.API.Services.IRepository
{
    public interface ICardRepository
    {
        public Task<Response> GetCardsAsync(int workspaceId);
    }
}