
using AutoMapper;
using TaskManager.API.Data;
using TaskManager.API.Data.DTOs;
using TaskManager.API.Data.Models;
using TaskManager.API.Services.IRepository;

namespace TaskManager.API.Services.Repository
{
    public class LabelRepository : ILabelRepository
    {

        private readonly DataContext _dataContext;
        private readonly IMapper _mapper;
        private readonly DapperContext _dapperContext;
        public LabelRepository(DataContext dataContext, IMapper mapper, DapperContext dapperContext)
        {
            _dataContext = dataContext;
            _mapper = mapper;
            _dapperContext = dapperContext;
        }
        
        public async Task<Response> CreateLabelAsync(LabelDto labelDto)
        {
            try
            {
                var label = _mapper.Map<LabelDto, Label>(labelDto);
                _dataContext.Labels.Add(label);
                var isSaved = await SaveChangeAsync();
                if (isSaved)
                {            
                    labelDto = _mapper.Map<Label, LabelDto>(label);
                    return new Response
                    {
                        Message = "Tạo nhãn thành công",
                        Data = new Dictionary<string, object>{
                            ["Label"] = labelDto,
                        }, 
                        IsSuccess = true
                    };
                }
                return new Response
                {
                    Message = "Tạo nhãn thất bại",
                    IsSuccess = false
                };
            }
            catch (Exception e)
            {
                Console.WriteLine("CreateLabelAsync " + e.Message);
                throw e;
            }
        }

        public async Task<Response> DeleteLabelAsync(int labelId)
        {
            try
            {
                var label = _dataContext.Labels.FirstOrDefault(l => l.Id == labelId);
                if(label == null){
                    return new Response
                    {
                        Message = "Không tìm thấy nhãn",
                        IsSuccess = false
                    };
                }
                _dataContext.Labels.Remove(label);
                var isSaved = await SaveChangeAsync();
                if (isSaved)
                {            
                    return new Response
                    {
                        Message = "Xóa nhãn thành công",
                        IsSuccess = true
                    };
                }
                return new Response
                {
                    Message = "Xóa nhãn thất bại",
                    IsSuccess = false
                };
            }
            catch (Exception e)
            {
                Console.WriteLine("CreateLabelAsync " + e.Message);
                throw e;
            }
        }

        public async Task<Response> GetListLabelByWorkspaceIdAsync(int workspaceId)
        {
            try{
                var labels = _dataContext.Labels.Where(l => l.WorkspaceId == workspaceId).ToList();
                if(labels == null)
                    return new Response
                    {
                        Message = "Không tìm thấy nhãn",
                        IsSuccess = false
                    };
                var labelDtos = _mapper.Map<List<Label>, List<LabelDto>>(labels);
                return new Response
                {
                    Message = "Lấy danh sách nhãn thành công",
                    Data = new Dictionary<string, object>{
                        ["Labels"] = labelDtos,
                    }, 
                    IsSuccess = true
                };
            }
            catch (Exception e)
            {
                Console.WriteLine("CreateLabelAsync " + e.Message);
                throw e;
            }
        }

        public async Task<Response> UpdateLabelAsync(int labelId, LabelDto labelDto)
        {
            try
            {
                var label = _dataContext.Labels.FirstOrDefault(l => l.Id == labelId);
                if(label == null)
                    return new Response
                    {
                        Message = "Không tìm thấy nhãn",
                        IsSuccess = false
                    };

                label.Name = labelDto.Name;
                label.Color = labelDto.Color;

                _dataContext.Labels.Update(label);
                var isSaved = await SaveChangeAsync();
                if (isSaved)
                {            
                    labelDto = _mapper.Map<Label, LabelDto>(label);
                    return new Response
                    {
                        Message = "Cập nhật nhãn thành công",
                        Data = new Dictionary<string, object>{
                            ["Label"] = labelDto,
                        }, 
                        IsSuccess = true
                    };
                }
                return new Response
                {
                    Message = "Cập nhật nhãn thất bại",
                    IsSuccess = false
                };
            }
            catch (Exception e)
            {
                Console.WriteLine("CreateLabelAsync " + e.Message);
                throw e;
            }
        }
         public async Task<bool> SaveChangeAsync()
        {
            return await _dataContext.SaveChangesAsync() > 0;        
        }
    }
}