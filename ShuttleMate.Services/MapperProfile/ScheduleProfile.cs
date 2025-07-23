using AutoMapper;
using ShuttleMate.Contract.Repositories.Entities;
using ShuttleMate.ModelViews.ScheduleModelViews;

namespace ShuttleMate.Services.MapperProfile
{
    public class ScheduleProfile : Profile
    {
        public ScheduleProfile()
        {
            CreateMap<Schedule, ResponseScheduleModel>().ReverseMap();
            CreateMap<Schedule, CreateScheduleModel>().ReverseMap();
            CreateMap<Schedule, UpdateScheduleModel>().ReverseMap();
        }
    }
}
