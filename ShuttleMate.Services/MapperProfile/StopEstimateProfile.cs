using AutoMapper;
using ShuttleMate.Contract.Repositories.Entities;
using ShuttleMate.ModelViews.StopEstimateModelViews;

namespace ShuttleMate.Services.MapperProfile
{
    public class StopEstimateProfile : Profile
    {
        public StopEstimateProfile()
        {
            CreateMap<StopEstimate, ResponseStopEstimateModel>()
                .ForMember(dest => dest.StopName, opt => opt.MapFrom(src => src.Stop.Name))
                .ReverseMap();
        }
    }
}
