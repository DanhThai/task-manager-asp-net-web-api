
using System.Data;
using AutoMapper;
using Dapper;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.SignalR;
using TaskManager.API.Data;
using TaskManager.API.Data.DTOs;
using TaskManager.API.Data.Models;
using TaskManager.API.Services.IRepository;

namespace TaskManager.API.Services.Repository
{
    public class ScheduleRepository : IScheduleRepository
    {
        private readonly DataContext _dataContext;
        private readonly IMapper _mapper;
        private IHubContext<HubService, IHubService> _hubService;
        private readonly DapperContext _dapperContext;


        public ScheduleRepository(DataContext dataContext, IMapper mapper, IHubContext<HubService, IHubService> hubService, DapperContext dapperContext)
        {
            _dataContext = dataContext;
            _mapper = mapper;
            _hubService = hubService;
            _dapperContext = dapperContext;
        }

        public async Task<Response> CreateScheduleAsync(string userId, ScheduleDto scheduleDto)
        {
            try
            {
                // Check user have permission to assign
                var mwAdmin = _dataContext.MemberWorkspaces.FirstOrDefault(
                    x => x.WorkspaceId == scheduleDto.WorkspaceId &&
                    x.UserId == userId);
                if (mwAdmin.Role == ROLE_ENUM.Member)
                    return new Response
                    {
                        Message = "Bạn không được phép tạo lịch vì bạn là thành viên",
                        IsSuccess = false
                    };

                var schedule = _mapper.Map<ScheduleDto, Schedule>(scheduleDto);
                schedule.CreatorId = userId;
                _dataContext.Schedules.Add(schedule);

                var activation = new Activation
                {
                    UserId = userId,
                    WorkspaceId = schedule.WorkspaceId,
                    Content = $"đã tạo lich {schedule.Title} từ {schedule.StartDateTime.ToString("dd/MM/yyyy HH:mm")} tới {schedule.EndDateTime.ToShortDateString()}",
                    CreateAt = DateTime.Now
                };
                await _dataContext.Activations.AddAsync(activation);

                var isSaved = await SaveChangeAsync();
                if (isSaved)
                {
                    // var activationDto = _mapper.Map<Activation, ActivationDto>(activation);
                    // await _hubService.Clients.Group($"Workspace-{schedule.WorkspaceId}").ActivationAsync(activationDto);

                    scheduleDto = _mapper.Map<Schedule, ScheduleDto>(schedule);
                    return new Response
                    {
                        Message = "Tạo lịch thành công",
                        Data = new Dictionary<string, object>{
                            ["Schedule"] = scheduleDto,
                        },
                        IsSuccess = true
                    };
                }
                return new Response
                {
                    Message = "Tạo lịch thất bại",
                    IsSuccess = false
                };
            }
            catch (Exception e)
            {
                Console.WriteLine("CreatescheduleAsync " + e.Message);
                throw e;
            }
        }

        public async Task<Response> DeleteScheduleAsync(int scheduleId, int workspaceId, string userId)
        {
            try
            {
                // Check user have permission to assign
                var mwAdmin = _dataContext.MemberWorkspaces.FirstOrDefault(
                    x => x.WorkspaceId == workspaceId &&
                    x.UserId == userId);
                if (mwAdmin.Role == ROLE_ENUM.Member)
                    return new Response
                    {
                        Message = "Bạn không được phép xóa lịch.",
                        IsSuccess = false
                    };
                var schedule = _dataContext.Schedules.FirstOrDefault(t => t.Id == scheduleId);
                if (schedule != null)
                {
                    _dataContext.Schedules.Remove(schedule);

                    var activation = new Activation
                    {
                        UserId = userId,
                        WorkspaceId = schedule.WorkspaceId,
                        Content = $"đã xóa lịch {schedule.Title}",
                        CreateAt = DateTime.Now
                    };
                    await _dataContext.Activations.AddAsync(activation);

                    var isSaved = await SaveChangeAsync();
                    if (isSaved)
                    {
                        // var activationDto = _mapper.Map<Activation, ActivationDto>(activation);
                        // await _hubService.Clients.Group($"Workspace-{schedule.WorkspaceId}").ActivationAsync(activationDto);

                        return new Response
                        {
                            Message = "Xóa lịch thành công",
                            IsSuccess = true
                        };
                    }
                    return new Response
                    {
                        Message = "Xóa lịch thất bại",
                        IsSuccess = false
                    };
                }
                return new Response
                {
                    Message = "Không tìm thấy lịch",
                    IsSuccess = false
                };

            }
            catch (Exception e)
            {
                Console.WriteLine("DeletescheduleAsync " + e.Message);
                throw e;
            }
        }

        public async Task<Response> GetSchedulesByWorkspaceAsync(int workspaceId)
        {
            try
            {
                var query = @"SELECT s.*, u.FullName, u.Email, u.Avatar
                              FROM Schedules s
                              INNER JOIN aspnetusers u on u.Id = s.CreatorId
                              WHERE s.WorkspaceId = @workspaceId;";

                var parameters = new DynamicParameters();
                parameters.Add("workspaceId", workspaceId, DbType.Int32);  
                var scheduleDtos = await _dapperContext.GetListAsync<ScheduleDto>(query, parameters);
                return new Response
                {
                    Message = "Lấy danh sách lịch thành công",
                    Data = new Dictionary<string, object>{
                        ["schedules"] =scheduleDtos
                    },
                    IsSuccess = false
                };
            }
            catch (Exception e)
            {
                Console.WriteLine("GetSchedulesByWorkspaceAsync " + e.Message);
                throw e;
            }
        }

        public Task<Response> GetScheduleByIdAsync(int scheduleId)
        {
            throw new NotImplementedException();
        }
        public async Task<Response> PatchScheduleAsync(int scheduleId, int workspaceId, string userId, JsonPatchDocument<Schedule> patchSchedule){
            try
            {
                // Check user have permission to assign
                var mwAdmin = _dataContext.MemberWorkspaces.FirstOrDefault(
                    x => x.WorkspaceId == workspaceId &&
                    x.UserId == userId);
                if (mwAdmin.Role == ROLE_ENUM.Member)
                    return new Response
                    {
                        Message = "Bạn không được phép chỉnh sửa lịch.",
                        IsSuccess = false
                    };

                var schedule = _dataContext.Schedules.FirstOrDefault(t => t.Id == scheduleId);
                if (schedule == null)
                    return new Response
                    {
                        Message = "Không tìm thấy lịch.",
                        IsSuccess = false
                    };
                patchSchedule.ApplyTo(schedule);
                
                _dataContext.Schedules.Update(schedule);

                // var activation = new Activation
                // {
                //     UserId = userId,
                //     WorkspaceId =schedule.WorkspaceId,
                //     Content = $"Cập nhật lịch {schedule.Title}",
                //     CreateAt = DateTime.Now
                // };
                // await _dataContext.Activations.AddAsync(activation);

                var isSaved = await SaveChangeAsync();
                if (isSaved)
                {
                    // var activationDto = _mapper.Map<Activation, ActivationDto>(activation);
                    // await _hubService.Clients.Group($"Workspace-{schedule.WorkspaceId}").SendActivationAsync(activationDto);

                    return new Response
                    {
                        Message = "Cập nhật lịch thành công.",
                        IsSuccess = true
                    };
                }
                return new Response
                {
                    Message = "Cập nhật lịch thất bại.",
                    IsSuccess = false
                };

            }
            catch (Exception e)
            {
                Console.WriteLine("UpdatescheduleAsync " + e.Message);
                throw e;
            }
        }

        public async Task<Response> UpdateScheduleAsync(int scheduleId, string userId, ScheduleDto scheduleDto)
        {
            try
            {
                // Check user have permission to assign
                var mwAdmin = _dataContext.MemberWorkspaces.FirstOrDefault(
                    x => x.WorkspaceId == scheduleDto.WorkspaceId &&
                    x.UserId == userId);
                if (mwAdmin.Role == ROLE_ENUM.Member)
                    return new Response
                    {
                        Message = "Bạn không được phép chỉnh sửa lịch.",
                        IsSuccess = false
                    };
                var schedule = _dataContext.Schedules.FirstOrDefault(t => t.Id == scheduleId);
                schedule.Color = scheduleDto.Color;
                schedule.Title = scheduleDto.Title;
                schedule.Description= scheduleDto.Description;
                schedule.StartDateTime = scheduleDto.StartDateTime;
                schedule.EndDateTime = scheduleDto.EndDateTime;
                
                _dataContext.Schedules.Update(schedule);

                var activation = new Activation
                {
                    UserId = userId,
                    WorkspaceId =schedule.WorkspaceId,
                    Content = $"đã cập nhật lịch {schedule.Title}",
                    CreateAt = DateTime.Now
                };
                await _dataContext.Activations.AddAsync(activation);

                var isSaved = await SaveChangeAsync();
                if (isSaved)
                {
                    // var activationDto = _mapper.Map<Activation, ActivationDto>(activation);
                    // await _hubService.Clients.Group($"workspace-{schedule.WorkspaceId}").ActivationAsync(activationDto);

                    return new Response
                    {
                        Message = "Cập nhật lịch thành công",
                        IsSuccess = true
                    };
                }
                return new Response
                {
                    Message = "Cập nhật lịch thất bại",
                    IsSuccess = false
                };

            }
            catch (Exception e)
            {
                Console.WriteLine("UpdatescheduleAsync " + e.Message);
                throw e;
            }
        }
        public async Task<bool> SaveChangeAsync()
        {
            return await _dataContext.SaveChangesAsync() > 0;
        }
    }
}