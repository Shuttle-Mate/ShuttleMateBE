using AutoMapper;
using ShuttleMate.Contract.Repositories.Entities;
using ShuttleMate.ModelViews.PromotionModelViews;
using static ShuttleMate.Contract.Repositories.Enum.GeneralEnum;

namespace ShuttleMate.Services.MapperProfile
{
    public class PromotionProfile : Profile
    {
        public PromotionProfile()
        {
            CreateMap<Promotion, ResponsePromotionModel>()
                .ForMember(dest => dest.Type, opt => opt.MapFrom(src => ConvertPromotionTypeToVietnameseString(src.Type)))
                .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.Description))
                .ForMember(dest => dest.DiscountPrice, opt => opt.MapFrom(src => src.DiscountPrice))
                .ForMember(dest => dest.DiscountPercent, opt => opt.MapFrom(src => src.DiscountPercent))
                .ForMember(dest => dest.LimitSalePrice, opt => opt.MapFrom(src => src.LimitSalePrice))
                .ForMember(dest => dest.EndDate, opt => opt.MapFrom(src => src.EndDate))
                .ForMember(dest => dest.UsingLimit, opt => opt.MapFrom(src => src.UsingLimit))
                .ForMember(dest => dest.UsedCount, opt => opt.MapFrom(src => src.UsedCount))
                .ForMember(dest => dest.IsExpiredOrReachLimit, opt => opt.MapFrom(src => src.IsExpiredOrReachLimit))
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
                .ReverseMap();
            CreateMap<Promotion, CreatePromotionModel>().ReverseMap();
        }

        private string ConvertPromotionTypeToVietnameseString(TypePromotionEnum type)
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
