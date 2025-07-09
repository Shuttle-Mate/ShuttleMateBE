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
                .ForMember(dest => dest.Category, opt => opt.MapFrom(src => ConvertSupportRequestCategoryToVietnameseString(src.Category)))
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => ConvertSupportRequestStatusToVietnameseString(src.Status)))
                .ForMember(dest => dest.Title, opt => opt.MapFrom(src => src.Title))
                .ForMember(dest => dest.Message, opt => opt.MapFrom(src => src.Message))
                .ForMember(dest => dest.UserId, opt => opt.MapFrom(src => src.UserId))
                .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => src.User.FullName))
                .ReverseMap();

            CreateMap<SupportRequest, CreateSupportRequestModel>().ReverseMap();
        }

        private string ConvertSupportRequestStatusToVietnameseString(SupportRequestStatusEnum status)
        {
            return status switch
            {
                SupportRequestStatusEnum.IN_PROGRESS => "IN_PROGRESS",
                SupportRequestStatusEnum.ESCALATED => "ESCALATED",
                SupportRequestStatusEnum.RESOLVED => "RESOLVED",
                SupportRequestStatusEnum.CANCELLED => "CANCELLED",
                _ => "UNKNOWN"
            };
        }

        private string ConvertSupportRequestCategoryToVietnameseString(SupportRequestCategoryEnum category)
        {
            return category switch
            {
                SupportRequestCategoryEnum.SHUTTLE_DELAY => "SHUTTLE_DELAY",
                SupportRequestCategoryEnum.SHUTTLE_NO_SHOW => "SHUTTLE_NO_SHOW",
                SupportRequestCategoryEnum.UNSAFE_DRIVING => "UNSAFE_DRIVING",
                SupportRequestCategoryEnum.PAYMENT_ISSUE => "PAYMENT_ISSUE",
                SupportRequestCategoryEnum.TICKET_NOT_RECEIVED => "TICKET_NOT_RECEIVED",
                SupportRequestCategoryEnum.APP_CRASH => "APP_CRASH",
                SupportRequestCategoryEnum.GPS_NOT_ACCURATE => "GPS_NOT_ACCURATE",
                SupportRequestCategoryEnum.NOTIFICATION_MISSING => "NOTIFICATION_MISSING",
                SupportRequestCategoryEnum.ACCOUNT_PROBLEM => "ACCOUNT_PROBLEM",
                SupportRequestCategoryEnum.FEATURE_REQUEST => "FEATURE_REQUEST",
                SupportRequestCategoryEnum.SERVICE_COMPLAINT => "SERVICE_COMPLAINT",
                SupportRequestCategoryEnum.DRIVER_COMPLAINT => "DRIVER_COMPLAINT",
                SupportRequestCategoryEnum.GENERAL_INQUIRY => "GENERAL_INQUIRY",
                SupportRequestCategoryEnum.OTHER => "OTHER",
                _ => "UNKNOWN"
            };
        }
    }
}
