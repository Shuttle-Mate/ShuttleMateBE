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
                .ForMember(dest => dest.UserId, opt => opt.MapFrom(src => src.UserId))
                .ReverseMap();

            CreateMap<Promotion, CreateDiscountPricePromotionModel>()
                .ForMember(dest => dest.Type, opt => opt.MapFrom(src => src.Type))
                .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.Description))
                .ForMember(dest => dest.DiscountPrice, opt => opt.MapFrom(src => src.DiscountPrice))
                .ForMember(dest => dest.DiscountPercent, opt => opt.MapFrom(src => src.DiscountPercent))
                .ForMember(dest => dest.LimitSalePrice, opt => opt.MapFrom(src => src.LimitSalePrice))
                .ForMember(dest => dest.EndDate, opt => opt.MapFrom(src => src.EndDate))
                .ForMember(dest => dest.UsingLimit, opt => opt.MapFrom(src => src.UsingLimit))
                .ForMember(dest => dest.UsedCount, opt => opt.MapFrom(src => src.UsedCount))
                .ForMember(dest => dest.IsExpiredOrReachLimit, opt => opt.MapFrom(src => src.IsExpiredOrReachLimit))
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
                .ForMember(dest => dest.UserId, opt => opt.MapFrom(src => src.UserId))
                .ReverseMap();
        }

        private string ConvertPromotionTypeToVietnameseString(TypePromotionEnum type)
        {
            return type switch
            {
                TypePromotionEnum.PercentageDiscount => "Giảm giá theo phần trăm (%)",
                TypePromotionEnum.FixedAmountDiscount => "Giảm giá theo số tiền cố định",
                TypePromotionEnum.FreeRide => "Miễn phí 1 hoặc nhiều lượt đi",
                TypePromotionEnum.FirstTimeUser => "Khuyến mãi lần đầu sử dụng",
                TypePromotionEnum.ScheduleBased => "Khuyến mãi vào khung giờ/tuyến/ngày cụ thể",
                TypePromotionEnum.EventPromotion => "Sự kiện đặc biệt (lễ hội, khai giảng, 20/11, ...)",
                TypePromotionEnum.Other => "Loại khuyến mãi khác (ghi chú rõ trong mô tả)",
                _ => "Không xác định"
            };
        }
    }
}
