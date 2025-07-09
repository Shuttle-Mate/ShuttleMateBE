using AutoMapper;
using ShuttleMate.Contract.Repositories.Entities;
using ShuttleMate.ModelViews.ResponseSupportModelViews;

namespace ShuttleMate.Services.MapperProfile
{
    public class ResponseSupportProfile : Profile
    {
        public ResponseSupportProfile()
        {
            CreateMap<ResponseSupport, ResponseResponseSupportModel>().ReverseMap();
            CreateMap<ResponseSupport, CreateResponseSupportModel>().ReverseMap();
        }
    }
}
