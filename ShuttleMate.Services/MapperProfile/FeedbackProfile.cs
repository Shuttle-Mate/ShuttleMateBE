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
                FeedbackCategoryEnum.LateArrival => "Xe đến trễ",
                FeedbackCategoryEnum.EarlyArrival => "Xe đến quá sớm",
                FeedbackCategoryEnum.MissedPickup => "Xe không đến đón",
                FeedbackCategoryEnum.UnsafeDriving => "Lái xe ẩu, vượt tốc độ",
                FeedbackCategoryEnum.VehicleCleanliness => "Vệ sinh xe không đảm bảo",
                FeedbackCategoryEnum.OvercrowdedBus => "Xe quá tải",
                FeedbackCategoryEnum.DriverBehavior => "Thái độ tài xế không tốt",
                FeedbackCategoryEnum.AppCrash => "Ứng dụng bị treo/crash",
                FeedbackCategoryEnum.GPSInaccuracy => "Vị trí xe không chính xác",
                FeedbackCategoryEnum.NotificationIssue => "Không nhận được thông báo",
                FeedbackCategoryEnum.UIUXIssue => "Giao diện khó sử dụng",
                FeedbackCategoryEnum.PaymentFailed => "Thanh toán thất bại",
                FeedbackCategoryEnum.IncorrectCharge => "Bị trừ tiền sai",
                FeedbackCategoryEnum.TicketNotReceived => "Không nhận được vé",
                FeedbackCategoryEnum.PromotionIssue => "Lỗi mã khuyến mãi",
                FeedbackCategoryEnum.GeneralSuggestion => "Góp ý chung",
                FeedbackCategoryEnum.Compliment => "Khen ngợi",
                FeedbackCategoryEnum.Complaint => "Khiếu nại không rõ nhóm",
                FeedbackCategoryEnum.Other => "Khác",
                _ => "Không xác định"
            };
        }
    }
}
