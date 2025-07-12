using AutoMapper;
using ShuttleMate.Contract.Repositories.Entities;
using ShuttleMate.ModelViews.SupportRequestModelViews;
using static ShuttleMate.Contract.Repositories.Enum.GeneralEnum;

namespace ShuttleMate.Services.MapperProfile
{
    public class SupportRequestProfile : Profile
    {
        public SupportRequestProfile()
        {
            CreateMap<SupportRequest, ResponseSupportRequestModel>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.Category, opt => opt.MapFrom(src => ConvertSupportRequestCategoryToUppercaseString(src.Category)))
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => ConvertSupportRequestStatusToUppercaseString(src.Status)))
                .ForMember(dest => dest.Title, opt => opt.MapFrom(src => src.Title))
                .ForMember(dest => dest.Message, opt => opt.MapFrom(src => src.Message))
                .ForMember(dest => dest.CreatedTime, opt => opt.MapFrom(src => src.CreatedTime))
                .ForMember(dest => dest.UserId, opt => opt.MapFrom(src => src.UserId))
                .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => src.User.FullName))
                .ReverseMap();

            CreateMap<SupportRequest, CreateSupportRequestModel>().ReverseMap();
        }

        private string ConvertSupportRequestStatusToUppercaseString(SupportRequestStatusEnum status)
        {
            return status switch
            {
                SupportRequestStatusEnum.IN_PROGRESS => "IN_PROGRESS",
                SupportRequestStatusEnum.RESPONDED => "RESPONDED",
                SupportRequestStatusEnum.RESOLVED => "RESOLVED",
                SupportRequestStatusEnum.CANCELLED => "CANCELLED",
                _ => "UNKNOWN"
            };
        }

        private string ConvertSupportRequestCategoryToUppercaseString(SupportRequestCategoryEnum category)
        {
            return category switch
            {
                SupportRequestCategoryEnum.TRANSPORT_ISSUE => "TRANSPORT_ISSUE",
                SupportRequestCategoryEnum.TECHNICAL_ISSUE => "TECHNICAL_ISSUE",
                SupportRequestCategoryEnum.PAYMENT_ISSUE => "PAYMENT_ISSUE",
                SupportRequestCategoryEnum.GENERAL_INQUIRY => "GENERAL_INQUIRY",
                SupportRequestCategoryEnum.OTHER => "OTHER",
                _ => "UNKNOWN"
            };
        }
    }
}
