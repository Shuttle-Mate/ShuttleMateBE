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
                .ForMember(dest => dest.PromotionType, opt => opt.MapFrom(src => src.Type.ToString()))
                .ForMember(dest => dest.ApplicableTicketType, opt => opt.MapFrom(src => src.ApplicableTicketType.HasValue ? src.ApplicableTicketType.Value.ToString() : null));
            CreateMap<CreatePromotionModel, Promotion>()
                .ForMember(dest => dest.Type, opt => opt.Ignore())
                .ForMember(dest => dest.ApplicableTicketType, opt => opt.Ignore())
                .ForMember(dest => dest.IsGlobal, opt => opt.Ignore())
                .ForMember(dest => dest.TicketId, opt => opt.Ignore());
            CreateMap<UpdatePromotionModel, Promotion>()
                .ForMember(dest => dest.Type, opt => opt.Ignore())
                .ForMember(dest => dest.ApplicableTicketType, opt => opt.Ignore())
                .ForMember(dest => dest.IsGlobal, opt => opt.Ignore())
                .ForMember(dest => dest.TicketId, opt => opt.Ignore());
        }
    }
}
