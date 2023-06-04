
using AutoMapper;
using TaskManager.API.Data.DTOs;
using TaskManager.API.Data.Models;

namespace TaskManager.API.Helper
{
    public class MapperProfile : Profile
    {
        public MapperProfile()
        {
            CreateMap<UserDto, Account>().ReverseMap();
            CreateMap<WorkspaceDto, Workspace>().ReverseMap();
            CreateMap<TaskItemDto, TaskItem>().ReverseMap();
            CreateMap<ActivationDto, Activation>().ReverseMap();
            CreateMap<SubtaskDto, Subtask>().ReverseMap();
            CreateMap<Schedule, ScheduleDto>().ReverseMap();
            CreateMap<Label, LabelDto>().ReverseMap();
            CreateMap<MemberTask, MemberTaskDto>().ReverseMap();
            CreateMap<MemberWorkspace, MemberWorkspaceDto>().ReverseMap();
            CreateMap<Comment, CommentDto>().ReverseMap();

            CreateMap<Card, CardDto>().ForMember(dest => dest.ListTaskIdOrder, 
                        op => op.MapFrom(src => src.TaskOrder.ConvertStringToList()));
        }
    }
}