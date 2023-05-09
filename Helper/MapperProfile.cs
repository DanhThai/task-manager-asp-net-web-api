
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

        }
    }
}