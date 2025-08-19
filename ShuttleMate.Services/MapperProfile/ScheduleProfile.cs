using AutoMapper;
using ShuttleMate.Contract.Repositories.Entities;
using ShuttleMate.ModelViews.ScheduleModelViews;

namespace ShuttleMate.Services.MapperProfile
{
    public class ScheduleProfile : Profile
    {
        public ScheduleProfile()
        {

            CreateMap<Schedule, ResponseOldScheduleModel>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.Direction, opt => opt.MapFrom(src => src.Direction.ToString()))
                .ForMember(dest => dest.DepartureTime, opt => opt.MapFrom(src => src.DepartureTime.ToString("HH:mm")))
                .ForMember(dest => dest.DayOfWeeks, opt => opt.MapFrom(src => DecodeBinaryDayOfWeek(src.DayOfWeek)))
                .ReverseMap();

            CreateMap<Schedule, ResponseScheduleDetailModel>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.DepartureTime, opt => opt.MapFrom(src => src.DepartureTime.ToString("HH:mm")))
                .ForMember(dest => dest.Direction, opt => opt.MapFrom(src => src.Direction.ToString()))
                .ForMember(dest => dest.Shuttle, opt => opt.MapFrom(src => src.Shuttle))
                .ForMember(dest => dest.Driver, opt => opt.MapFrom(src => src.Driver))
                .ForMember(dest => dest.SchoolShift, opt => opt.MapFrom(src => src.SchoolShift))
                .ReverseMap();

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

        private static IEnumerable<ResponseDayOfWeekModel> DecodeBinaryDayOfWeek(string binaryDayOfWeek)
        {
            var dayNames = new[] { "MONDAY", "TUESDAY", "WEDNESDAY", "THURSDAY", "FRIDAY", "SATURDAY", "SUNDAY" };
            var result = new List<ResponseDayOfWeekModel>();

            for (int i = 0; i < binaryDayOfWeek.Length && i < 7; i++)
            {
                if (binaryDayOfWeek[i] == '1')
                {
                    result.Add(new ResponseDayOfWeekModel { DayOfWeek = dayNames[i] });
                }
            }

            return result;
        }
    }
}
