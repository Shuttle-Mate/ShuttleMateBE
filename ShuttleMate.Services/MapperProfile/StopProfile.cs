using AutoMapper;
using ShuttleMate.Contract.Repositories.Entities;
using ShuttleMate.ModelViews.StopModelViews;

namespace ShuttleMate.Services.MapperProfile
{
    public class StopProfile : Profile
    {
        public StopProfile()
        {
            CreateMap<Stop, ResponseStopModel>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
                .ForMember(dest => dest.Address, opt => opt.MapFrom(src => src.Address))
                .ForMember(dest => dest.Lat, opt => opt.MapFrom(src => src.Lat))
                .ForMember(dest => dest.Lng, opt => opt.MapFrom(src => src.Lng))
                .ForMember(dest => dest.WardId, opt => opt.MapFrom(src => src.WardId))
                .ForMember(dest => dest.WardName, opt => opt.MapFrom(src => src.Ward.Name))
                .ReverseMap();
            CreateMap<Stop, CreateStopModel>().ReverseMap();
            CreateMap<Stop, UpdateStopModel>().ReverseMap();
        }
    }
}
