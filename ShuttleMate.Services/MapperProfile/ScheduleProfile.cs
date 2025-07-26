using AutoMapper;
using ShuttleMate.Contract.Repositories.Entities;
using ShuttleMate.ModelViews.ScheduleModelViews;

namespace ShuttleMate.Services.MapperProfile
{
    public class ScheduleProfile : Profile
    {
        public ScheduleProfile()
        {
            CreateMap<Schedule, ResponseScheduleModel>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.DepartureTime, opt => opt.MapFrom(src => src.DepartureTime.ToString("HH:mm")))
                .ForMember(dest => dest.Direction, opt => opt.MapFrom(src => src.Direction.ToString()))
                .ForMember(dest => dest.DayOfWeeks, opt => opt.MapFrom(src => DecodeBinaryDayOfWeek(src.DayOfWeek)));

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
            
            CreateMap<Schedule, CreateScheduleModel>().ReverseMap();

            CreateMap<Schedule, UpdateScheduleModel>().ReverseMap();
        }

        private IEnumerable<ResponseDayOfWeekModel> DecodeBinaryDayOfWeek(string binaryDayOfWeek)
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
