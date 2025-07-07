using AutoMapper;
using ShuttleMate.Contract.Repositories.Entities;
using ShuttleMate.ModelViews.FeedbackModelViews;
using static ShuttleMate.Contract.Repositories.Enum.GeneralEnum;

namespace ShuttleMate.Services.MapperProfile
{
    public class FeedbackProfile : Profile
    {
        public FeedbackProfile()
        {
            CreateMap<Feedback, ResponseFeedbackModel>()
                .ForMember(dest => dest.FeedbackCategory, opt => opt.MapFrom(src => ConvertFeedbackCategoryToVietnameseString(src.FeedbackCategory)))
                .ForMember(dest => dest.Message, opt => opt.MapFrom(src => src.Message))
                .ForMember(dest => dest.Rating, opt => opt.MapFrom(src => src.Rating))
                .ForMember(dest => dest.UserId, opt => opt.MapFrom(src => src.UserId))
                .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => src.User.FullName))
                .ReverseMap();
            CreateMap<Feedback, CreateFeedbackModel>()
                .ForMember(dest => dest.FeedbackCategory, opt => opt.MapFrom(src => src.FeedbackCategory))
                .ForMember(dest => dest.Message, opt => opt.MapFrom(src => src.Message))
                .ForMember(dest => dest.Rating, opt => opt.MapFrom(src => src.Rating))
                .ReverseMap();
        }

        private string ConvertFeedbackCategoryToVietnameseString(FeedbackCategoryEnum category)
        {
            return category switch
            {
                FeedbackCategoryEnum.LATE_ARRIVAL => "LATE_ARRIVAL",
                FeedbackCategoryEnum.EARLY_ARRIVAL => "EARLY_ARRIVAL",
                FeedbackCategoryEnum.MISSED_PICKUP => "MISSED_PICKUP",
                FeedbackCategoryEnum.UNSAFE_DRIVING => "UNSAFE_DRIVING",
                FeedbackCategoryEnum.VEHICLE_CLEANLINESS => "VEHICLE_CLEANLINESS",
                FeedbackCategoryEnum.OVERCROWDED_BUS => "OVERCROWDED_BUS",
                FeedbackCategoryEnum.DRIVER_BEHAVIOR => "DRIVER_BEHAVIOR",

                FeedbackCategoryEnum.APP_CRASH => "APP_CRASH",
                FeedbackCategoryEnum.GPS_INACCURACY => "GPS_INACCURACY",
                FeedbackCategoryEnum.NOTIFICATION_ISSUE => "NOTIFICATION_ISSUE",
                FeedbackCategoryEnum.UI_UX_ISSUE => "UI_UX_ISSUE",

                FeedbackCategoryEnum.PAYMENT_FAILED => "PAYMENT_FAILED",
                FeedbackCategoryEnum.INCORRECT_CHARGE => "INCORRECT_CHARGE",
                FeedbackCategoryEnum.TICKET_NOT_RECEIVED => "TICKET_NOT_RECEIVED",
                FeedbackCategoryEnum.PROMOTION_ISSUE => "PROMOTION_ISSUE",

                FeedbackCategoryEnum.GENERAL_SUGGESTION => "GENERAL_SUGGESTION",
                FeedbackCategoryEnum.COMPLIMENT => "COMPLIMENT",
                FeedbackCategoryEnum.COMPLAINT => "COMPLAINT",
                FeedbackCategoryEnum.OTHER => "OTHER",

                _ => "UNKNOWN"
            };
        }
    }
}
