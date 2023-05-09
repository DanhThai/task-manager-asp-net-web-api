
using System.Data;
using AutoMapper;
using Dapper;
using TaskManager.API.Data;
using TaskManager.API.Data.DTOs;
using TaskManager.API.Services.IRepository;

namespace TaskManager.API.Services.Repository
{
    public class CardRepository : ICardRepository
    {
        
        private readonly IMapper _mapper;
        private readonly DapperContext _dapperContext;

        public CardRepository(DapperContext dapperContext, IMapper mapper)
        {
            _dapperContext = dapperContext;
            _mapper = mapper;
        }

        public async Task<Response> GetCardsAsync(int workspaceId)
        {
            try{
                var query = @"SELECT c.Id, c.Name, c.Code, c.TaskOrder,
                                    t.Id, t.Title, t.Description, t.Priority, t.DueDate
                              FROM Cards c  
                              LEFT JOIN TaskItems t on c.Id = t.CardId 
                              WHERE c.WorkspaceId = @WorkspaceId;";

                var parameters = new DynamicParameters();
                parameters.Add("WorkspaceId", workspaceId, DbType.Int32);  

                var cardDict = new Dictionary<int, CardDto>();
                using (var connection = _dapperContext.CreateConnection())
                {
                    var multiResult = await connection.QueryAsync<CardDto,TaskItemDto,CardDto>(
                    query, (card, taskItem)=>{
                        if(!cardDict.TryGetValue(card.Id, out var currentCard)){
                            currentCard = card;
                            cardDict.Add(card.Id, currentCard);
                        }
                        currentCard.TaskItems.Add(taskItem);
                        return currentCard;
                    }, 
                    parameters);
                }
                if (cardDict.Count()<=0){
                    return new Response{
                        Message = "Not found cards",
                        IsSuccess = false
                    };
                }

                return new Response{
                    Message = "Get workspace successfully",
                    Data = new Dictionary<string, object>{
                        ["Cards"] = cardDict
                    },
                    IsSuccess = true
                };
                
            }
            catch (Exception e){
                throw e;
            }
            throw new NotImplementedException();
        }

    }
}