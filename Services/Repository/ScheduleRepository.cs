
using AutoMapper;
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

        public ScheduleRepository(DataContext dataContext, IMapper mapper, IHubContext<HubService, IHubService> hubService)
        {
            _dataContext = dataContext;
            _mapper = mapper;
            _hubService = hubService;
        }

        public async Task<Response> CreateScheduleAsync(string userId, ScheduleDto scheduleDto)
        {
            try
            {
                var schedule = _mapper.Map<ScheduleDto, Schedule>(scheduleDto);
                _dataContext.Schedules.Add(schedule);

                var activation = new Activation
                {
                    UserId = userId,
                    WorkspaceId = schedule.WorkspaceId,
                    Content = $"Create schedule {schedule.Title} at {schedule.Date.ToShortDateString()}",
                    CreateAt = DateTime.Now
                };
                await _dataContext.Activations.AddAsync(activation);

                var isSaved = await SaveChangeAsync();
                if (isSaved)
                {
                    var activationDto = _mapper.Map<Activation, ActivationDto>(activation);
                    await _hubService.Clients.Group($"Workspace-{schedule.WorkspaceId}").SendActivationAsync(activationDto);

                    scheduleDto = _mapper.Map<Schedule, ScheduleDto>(schedule);
                    return new Response
                    {
                        Message = "Created schedule is succeed",
                        Data = new Dictionary<string, object>{
                            ["Schedule"] = scheduleDto,
                        },
                        IsSuccess = true
                    };
                }
                return new Response
                {
                    Message = "Created schedule is failed",
                    IsSuccess = false
                };
            }
            catch (Exception e)
            {
                Console.WriteLine("CreatescheduleAsync " + e.Message);
                throw e;
            }
        }

        public async Task<Response> DeleteScheduleAsync(int scheduleId, string userId)
        {
            try
            {
                var schedule = _dataContext.Schedules.FirstOrDefault(t => t.Id == scheduleId);
                if (schedule != null)
                {
                    _dataContext.Schedules.Remove(schedule);

                    var activation = new Activation
                    {
                        UserId = userId,
                        WorkspaceId = schedule.WorkspaceId,
                        Content = $"Remove schedule {schedule.Title}",
                        CreateAt = DateTime.Now
                    };
                    await _dataContext.Activations.AddAsync(activation);

                    var isSaved = await SaveChangeAsync();
                    if (isSaved)
                    {
                        var activationDto = _mapper.Map<Activation, ActivationDto>(activation);
                        await _hubService.Clients.Group($"Workspace-{schedule.WorkspaceId}").SendActivationAsync(activationDto);

                        return new Response
                        {
                            Message = "Deleted schedule is succeed",
                            IsSuccess = true
                        };
                    }
                    return new Response
                    {
                        Message = "Remove schedule is failed",
                        IsSuccess = false
                    };
                }
                return new Response
                {
                    Message = "Not Found schedule",
                    IsSuccess = false
                };

            }
            catch (Exception e)
            {
                Console.WriteLine("DeletescheduleAsync " + e.Message);
                throw e;
            }
        }

        public Task<Response> GetScheduleByIdAsync(int scheduleId)
        {
            throw new NotImplementedException();
        }

        public async Task<bool> SaveChangeAsync()
        {
            return await _dataContext.SaveChangesAsync() > 0;
        }

        public async Task<Response> UpdateScheduleAsync(int scheduleId, string userId, ScheduleDto scheduleDto)
        {
            try
            {
                var schedule = _dataContext.Schedules.FirstOrDefault(t => t.Id == scheduleId);
                schedule.Color = scheduleDto.Color;
                schedule.Title = scheduleDto.Title;
                schedule.Date = scheduleDto.Date;
                
                _dataContext.Schedules.Update(schedule);

                var activation = new Activation
                {
                    UserId = userId,
                    WorkspaceId =schedule.WorkspaceId,
                    Content = $"Update schedule {schedule.Title}",
                    CreateAt = DateTime.Now
                };
                await _dataContext.Activations.AddAsync(activation);

                var isSaved = await SaveChangeAsync();
                if (isSaved)
                {
                    var activationDto = _mapper.Map<Activation, ActivationDto>(activation);
                    await _hubService.Clients.Group($"Workspace-{schedule.WorkspaceId}").SendActivationAsync(activationDto);

                    return new Response
                    {
                        Message = "Update schedule is succeed",
                        IsSuccess = true
                    };
                }
                return new Response
                {
                    Message = "Remove schedule is failed",
                    IsSuccess = false
                };

            }
            catch (Exception e)
            {
                Console.WriteLine("UpdatescheduleAsync " + e.Message);
                throw e;
            }
        }
    }
}