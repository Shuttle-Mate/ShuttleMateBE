using AutoMapper;
using ShuttleMate.Contract.Repositories.Entities;
using ShuttleMate.Core.Utils;
using ShuttleMate.ModelViews.PromotionModelViews;
using static ShuttleMate.Contract.Repositories.Enum.GeneralEnum;

namespace ShuttleMate.Services.MapperProfile
{
    public class PromotionProfile : Profile
    {
        public PromotionProfile()
        {
            CreateMap<Promotion, ResponsePromotionModel>()
                .ForMember(dest => dest.Type, opt => opt.MapFrom(src => src.Type.ToString().ToUpper()));
            CreateMap<Promotion, CreatePromotionModel>().ReverseMap();
            CreateMap<Promotion, UpdatePromotionModel>().ReverseMap();
        }

        private string ConvertPromotionTypeToUppercaseString(TypePromotionEnum type)
        {
            return type switch
            {
                TypePromotionEnum.PERCENTAGE_DISCOUNT => "PERCENTAGE_DISCOUNT",
                TypePromotionEnum.DIRECT_DISCOUNT => "DIRECT_DISCOUNT",
                TypePromotionEnum.FIXED_AMOUNT_DISCOUNT => "FIXED_AMOUNT_DISCOUNT",
                _ => "UNKNOWN"
            };
        }
    }
}
