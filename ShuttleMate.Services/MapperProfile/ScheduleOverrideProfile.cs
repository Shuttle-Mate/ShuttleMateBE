using AutoMapper;
using ShuttleMate.Contract.Repositories.Entities;
using ShuttleMate.ModelViews.ScheduleOverrideModelView;

namespace ShuttleMate.Services.MapperProfile
{
    public class ScheduleOverrideProfile : Profile
    {
        public ScheduleOverrideProfile()
        {
            CreateMap<CreateScheduleOverrideModel, ScheduleOverride>()
                .ForMember(dest => dest.OriginalShuttleId, opt => opt.Ignore())
                .ForMember(dest => dest.OriginalUserId, opt => opt.Ignore());
        }
    }
}
