using AutoMapper;
using ShuttleMate.Contract.Repositories.Entities;
using ShuttleMate.ModelViews.DepartureTimeModelViews;

namespace ShuttleMate.Services.MapperProfile
{
    public class DepartureTimeProfile : Profile
    {
        public DepartureTimeProfile()
        {
            CreateMap<DepartureTime, ResponseDepartureTimeModel>().ReverseMap();
            CreateMap<DepartureTime, CreateDepartureTimeModel>().ReverseMap();
            CreateMap<DepartureTime, UpdateDepartureTimeModel>().ReverseMap();
        }
    }
}
