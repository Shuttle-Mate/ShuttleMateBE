using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShuttleMate.Contract.Repositories.Enum
{
    public class GeneralEnum
    {
        public enum TripDirectionEnum
        {
            InBound,
            OutBound
        }
        public enum TripStatusEnum
        {
            Scheduled, //Scheduled but not start yet
            InProgress, 
            Completed,
            Cancelled//Hủy
        }
        public enum AttendanceStatusEnum
        {
            NotCheckedIn,
            CheckedIn,
            CheckedOut
        }
        public enum ChatBotRoleEnum
        {
            User,
            Bot
        }
        public enum FeedbackCategoryEnum
        {
            //Vận hành xe buýt
            LateArrival,              // Xe đến trễ
            EarlyArrival,             // Xe đến quá sớm
            MissedPickup,             // Xe không đến đón
            UnsafeDriving,            // Lái xe ẩu, vượt tốc độ
            VehicleCleanliness,       // Vệ sinh xe không đảm bảo
            OvercrowdedBus  ,           // Xe quá tải
            DriverBehavior,           // Thái độ tài xế không tốt

            //Ứng dụng & kỹ thuật
            AppCrash    ,                // Ứng dụng bị treo/crash
            GPSInaccuracy,           // Vị trí xe không chính xác
            NotificationIssue,       // Không nhận được thông báo
            UIUXIssue,               // Giao diện khó dùng

            //Thanh toán & vé
            PaymentFailed,           // Thanh toán thất bại
            IncorrectCharge,         // Bị trừ tiền sai
            TicketNotReceived,       // Không nhận được vé
            PromotionIssue,          // Lỗi mã khuyến mãi

            //Phản hồi chung
            GeneralSuggestion,       // Góp ý chung
            Compliment,              // Khen ngợi
            Complaint,               // Khiếu nại không rõ nhóm
            Other                    // Khác
        }
        public enum NotificationStatusEnum
        {
            Pending,         //Đã tạo nhưng chưa gửi
            Sent,            //Đã gửi thành công (đẩy qua push, email, v.v.)
            Delivered,       //Đã nhận ở thiết bị người dùng (nếu có tracking)
            Read,            //Người dùng đã mở và đọc thông báo
            Failed,          //Gửi thất bại (VD: push token hết hạn)
            Archived,        //Đã lưu trữ, không hiển thị trên UI
            Canceled         //Đã hủy (VD: hủy gửi trước giờ chạy)
        }
        public enum TypePromotionEnum
        {
            PercentageDiscount,        // Giảm giá theo phần trăm (%)
            FixedAmountDiscount,       // Giảm giá theo số tiền cố định
            FreeRide,                  // Miễn phí 1 hoặc nhiều lượt đi
            FirstTimeUser,             // Khuyến mãi lần đầu sử dụng
            ScheduleBased,            // Khuyến mãi vào khung giờ/tuyến/ngày cụ thể
            EventPromotion,          // Sự kiện đặc biệt (lễ hội, khai giảng, 20/11, ...)
            Other                    // Loại khuyến mãi khác (ghi chú rõ trong mô tả)
        }
        public enum ResponseSupportEnum
        {
            Resolved,             // Đã xử lý xong và giải quyết thành công
            Rejected,             // Từ chối xử lý (không hợp lệ hoặc không thuộc phạm vi)
            Escalated,            // Đã chuyển tiếp đến cấp cao hơn để xử lý
            PendingCustomer,      // Đang chờ người dùng cung cấp thêm thông tin
            PendingSupport,       // Đang chờ nhân viên/phòng ban xử lý
            CancelledByUser,      // Người dùng đã hủy yêu cầu
            TimedOut,             // Hết thời gian xử lý mà không có phản hồi (SLA breach)
            Other                // Trạng thái khác
        }
        public enum SupportRequestStatusEnum
        {
            Created,            // 🆕 Mới được tạo, chưa ai xử lý
            Open,               // 🔧 Đã được mở để xử lý (trạng thái làm việc)
            InProgress,         // 🛠️ Đang được xử lý bởi nhân viên kỹ thuật
            WaitingForCustomer,// ⏳ Đang chờ người dùng bổ sung thông tin
            WaitingForSupport, // ⌛ Đang chờ bộ phận khác/phản hồi nội bộ
            Escalated,          // ⚠️ Đã được chuyển cấp xử lý cao hơn
            Resolved,           // ✅ Đã giải quyết
            Closed,             // 📦 Đã đóng (hoàn tất)
            Cancelled           // ❌ Bị hủy bởi người dùng hoặc hệ thống
        }
        public enum SupportRequestCategoryEnum
        {
            ShuttleDelay,             // Xe đưa đón đến trễ
            ShuttleNoShow,            // Xe không đến đón
            UnsafeDriving,            // Tài xế lái ẩu
            PaymentIssue,             // Sự cố thanh toán
            TicketNotReceived,        // Không nhận được vé sau khi thanh toán
            AppCrash,                 // Ứng dụng bị lỗi hoặc treo
            GPSNotAccurate,           // Vị trí xe không chính xác
            NotificationMissing,      // Không nhận được thông báo
            AccountProblem,           // Không đăng nhập được, quên mật khẩu
            FeatureRequest,          // Góp ý thêm chức năng mới
            ServiceComplaint,        // Phàn nàn về chất lượng dịch vụ
            DriverComplaint,         // Phản ánh thái độ tài xế
            GeneralInquiry,          // Câu hỏi chung
            Other                    // Khác
        }
        public enum TicketTypeEnum
        {
            SingleRide,
            DayPass,
            Weekly,
            Monthly
        }
        public enum PaymentMethodEnum
        {
            PayOs,
            VNPay
        }
    }
}
