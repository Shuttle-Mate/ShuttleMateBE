using AutoMapper;
using ShuttleMate.Contract.Repositories.Entities;
using ShuttleMate.ModelViews.ScheduleModelViews;

namespace ShuttleMate.Services.MapperProfile
{
    public class ScheduleProfile : Profile
    {
        public ScheduleProfile()
        {
            CreateMap<Schedule, ResponseScheduleDetailModel>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.DepartureTime, opt => opt.MapFrom(src => src.DepartureTime.ToString("HH:mm")))
                .ForMember(dest => dest.Direction, opt => opt.MapFrom(src => src.Direction.ToString()))
                .ForMember(dest => dest.Shuttle, opt => opt.MapFrom(src => src.Shuttle))
                .ForMember(dest => dest.Driver, opt => opt.MapFrom(src => src.Driver))
                .ForMember(dest => dest.SchoolShift, opt => opt.MapFrom(src => src.SchoolShift));

            CreateMap<Shuttle, ResponseShuttleScheduleModel>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
                .ReverseMap();

            CreateMap<User, ResponseDriverScheduleModel>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => src.FullName))
                .ReverseMap();

            CreateMap<SchoolShift, ResponseSchoolShiftScheduleModel>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.SessionType, opt => opt.MapFrom(src => src.SessionType.ToString()))
                .ForMember(dest => dest.ShiftType, opt => opt.MapFrom(src => src.ShiftType.ToString()))
                .ReverseMap();
        }
    }
}
