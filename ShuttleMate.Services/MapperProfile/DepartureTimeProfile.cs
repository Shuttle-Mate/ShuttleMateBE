using AutoMapper;
using ShuttleMate.Contract.Repositories.Entities;
using ShuttleMate.ModelViews.DepartureTimeModelViews;

namespace ShuttleMate.Services.MapperProfile
{
    public class DepartureTimeProfile : Profile
    {
        public DepartureTimeProfile()
        {
            CreateMap<Schedule, ResponseDepartureTimeModel>().ReverseMap();
            CreateMap<Schedule, CreateDepartureTimeModel>().ReverseMap();
            CreateMap<Schedule, UpdateDepartureTimeModel>().ReverseMap();
        }
    }
}
