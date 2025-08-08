using AutoMapper;
using ShuttleMate.Contract.Repositories.Entities;
using ShuttleMate.ModelViews.PromotionModelViews;

namespace ShuttleMate.Services.MapperProfile
{
    public class PromotionProfile : Profile
    {
        public PromotionProfile()
        {
            CreateMap<Promotion, ResponsePromotionModel>()
                .ForMember(dest => dest.PromotionType, opt => opt.MapFrom(src => src.Type.ToString().ToUpper()));
            CreateMap<Promotion, CreatePromotionModel>().ReverseMap();
            CreateMap<Promotion, UpdatePromotionModel>().ReverseMap();
        }
    }
}
