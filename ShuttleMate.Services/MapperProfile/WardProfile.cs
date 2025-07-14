using AutoMapper;
using ShuttleMate.Contract.Repositories.Entities;
using ShuttleMate.ModelViews.WardModelViews;

namespace ShuttleMate.Services.MapperProfile
{
    public class WardProfile : Profile
    {
        public WardProfile()
        {
            CreateMap<Ward, ResponseWardModel>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
                .ReverseMap();
        }
    }
}
