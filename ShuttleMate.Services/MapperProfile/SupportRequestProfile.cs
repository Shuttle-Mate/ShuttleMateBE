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
                .ReverseMap();

            CreateMap<SupportRequest, CreateSupportRequestModel>().ReverseMap();
        }

        private string ConvertSupportRequestStatusToVietnameseString(SupportRequestStatusEnum status)
        {
            return status switch
            {
                SupportRequestStatusEnum.Created => "CREATED",
                SupportRequestStatusEnum.InProgress => "INPROGRESS",
                SupportRequestStatusEnum.Escalated => "ESCALATED",
                SupportRequestStatusEnum.Resolved => "RESOLVED",
                SupportRequestStatusEnum.Cancelled => "CANCELLED",
                _ => "UNKNOWN"
            };
        }

        private string ConvertSupportRequestCategoryToVietnameseString(SupportRequestCategoryEnum category)
        {
            return category switch
            {
                SupportRequestCategoryEnum.ShuttleDelay => "SHUTTLEDELAY",
                SupportRequestCategoryEnum.ShuttleNoShow => "SHUTTLENOSHOW",
                SupportRequestCategoryEnum.UnsafeDriving => "UNSAFEDRIVING",
                SupportRequestCategoryEnum.PaymentIssue => "PAYMENTISSUE",
                SupportRequestCategoryEnum.TicketNotReceived => "TICKETNOTRECEIVED",
                SupportRequestCategoryEnum.AppCrash => "APPCRASH",
                SupportRequestCategoryEnum.GPSNotAccurate => "GPSNOTACCURATE",
                SupportRequestCategoryEnum.NotificationMissing => "NOTIFICATIONMISSING",
                SupportRequestCategoryEnum.AccountProblem => "ACCOUNTPROBLEM",
                SupportRequestCategoryEnum.FeatureRequest => "FEATUREREQUEST",
                SupportRequestCategoryEnum.ServiceComplaint => "SERVICECOMPLAINT",
                SupportRequestCategoryEnum.DriverComplaint => "DRIVERCOMPLAINT",
                SupportRequestCategoryEnum.GeneralInquiry => "GENERALINQUIRY",
                SupportRequestCategoryEnum.Other => "OTHER",
                _ => "UNKNOWN"
            };
        }

    }
}
