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
                SupportRequestStatusEnum.Created => "Mới tạo",
                SupportRequestStatusEnum.Open => "Đã mở để xử lý",
                SupportRequestStatusEnum.InProgress => "Đang xử lý",
                SupportRequestStatusEnum.WaitingForCustomer => "Đang chờ người dùng bổ sung",
                SupportRequestStatusEnum.WaitingForSupport => "Đang chờ phản hồi bộ phận khác",
                SupportRequestStatusEnum.Escalated => "Đã chuyển cấp xử lý",
                SupportRequestStatusEnum.Resolved => "Đã giải quyết",
                SupportRequestStatusEnum.Closed => "Đã đóng",
                SupportRequestStatusEnum.Cancelled => "Đã hủy",
                _ => "Không xác định"
            };
        }

        private string ConvertSupportRequestCategoryToVietnameseString(SupportRequestCategoryEnum category)
        {
            return category switch
            {
                SupportRequestCategoryEnum.ShuttleDelay => "Xe đưa đón đến trễ",
                SupportRequestCategoryEnum.ShuttleNoShow => "Xe không đến đón",
                SupportRequestCategoryEnum.UnsafeDriving => "Tài xế lái ẩu",
                SupportRequestCategoryEnum.PaymentIssue => "Sự cố thanh toán",
                SupportRequestCategoryEnum.TicketNotReceived => "Không nhận được vé",
                SupportRequestCategoryEnum.AppCrash => "Ứng dụng bị lỗi hoặc treo",
                SupportRequestCategoryEnum.GPSNotAccurate => "Vị trí xe không chính xác",
                SupportRequestCategoryEnum.NotificationMissing => "Không nhận được thông báo",
                SupportRequestCategoryEnum.AccountProblem => "Vấn đề tài khoản",
                SupportRequestCategoryEnum.FeatureRequest => "Yêu cầu tính năng mới",
                SupportRequestCategoryEnum.ServiceComplaint => "Phàn nàn dịch vụ",
                SupportRequestCategoryEnum.DriverComplaint => "Phản ánh tài xế",
                SupportRequestCategoryEnum.GeneralInquiry => "Câu hỏi chung",
                SupportRequestCategoryEnum.Other => "Khác",
                _ => "Không xác định"
            };
        }

    }
}
