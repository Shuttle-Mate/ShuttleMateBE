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
                .ForMember(dest => dest.TicketTypes, opt => opt.MapFrom(src =>
                    src.TicketPromotions.Select(tp => new ResponseTicketPromotionModel
                    {
                        TicketId = tp.TicketId,
                        Type = tp.TicketType != null
                            ? tp.TicketType.Type.ToString().ToUpper()
                            : string.Empty
                    }).ToList()
                    ));

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
