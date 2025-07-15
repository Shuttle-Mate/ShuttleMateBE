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
            // Vận hành xe buýt
            LATE_ARRIVAL,              // Xe đến trễ
            EARLY_ARRIVAL,             // Xe đến quá sớm
            MISSED_PICKUP,             // Xe không đến đón
            UNSAFE_DRIVING,            // Lái xe ẩu, vượt tốc độ
            VEHICLE_CLEANLINESS,       // Vệ sinh xe không đảm bảo
            OVERCROWDED_BUS,           // Xe quá tải
            DRIVER_BEHAVIOR,           // Thái độ tài xế không tốt

            // Ứng dụng & kỹ thuật
            APP_CRASH,                 // Ứng dụng bị treo/crash
            GPS_INACCURACY,            // Vị trí xe không chính xác
            NOTIFICATION_ISSUE,        // Không nhận được thông báo
            UI_UX_ISSUE,               // Giao diện khó dùng

            // Thanh toán & vé
            PAYMENT_FAILED,            // Thanh toán thất bại
            INCORRECT_CHARGE,          // Bị trừ tiền sai
            TICKET_NOT_RECEIVED,       // Không nhận được vé
            PROMOTION_ISSUE,           // Lỗi mã khuyến mãi

            // Phản hồi chung
            GENERAL_SUGGESTION,        // Góp ý chung
            COMPLIMENT,                // Khen ngợi
            COMPLAINT,                 // Khiếu nại không rõ nhóm
            OTHER                      // Khác
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
            PERCENTAGE_DISCOUNT,         // Giảm giá theo phần trăm (%)
            DIRECT_DISCOUNT,             // Giảm giá trực tiếp
            FIXED_AMOUNT_DISCOUNT        // Giảm giá theo số tiền
        }

        public enum SupportRequestStatusEnum
        {
            IN_PROGRESS,         // Đang được xử lý
            RESPONDED,           // Đã phản hồi
            RESOLVED,            // Đã giải quyết
            CANCELLED            // Bị hủy bởi người dùng
        }

        public enum SupportRequestCategoryEnum
        {
            TRANSPORT_ISSUE,       // Vấn đề xe đưa đón (xe đến trễ, không đến, lái xe nguy hiểm)
            TECHNICAL_ISSUE,       // Lỗi kỹ thuật (app treo, GPS sai, không nhận được thông báo, lỗi tài khoản)
            PAYMENT_ISSUE,         // Sự cố thanh toán hoặc không nhận được vé
            GENERAL_INQUIRY,       // Câu hỏi chung, không thuộc vấn đề cụ thể
            OTHER                  // Khác
        }

        public enum WithdrawalRequestStatusEnum
        {
            IN_PROGRESS,         // Đang chờ duyệt
            COMPLETED,           // Đã hoàn thành
            REJECTED,            // Bị từ chối
        }

        public enum TicketTypeEnum
        {
            SINGLE_RIDE,
            DAY_PASS,
            WEEKLY,
            MONTHLY,
            SEMESTER
        }

        public enum PaymentMethodEnum
        {
            PAYOS,
            VNPAY
            
        }

        public enum PaymentStatus
        {
            UNPAID = 0,
            PAID = 1,
            REFUNDED = 2,
            CANCELED = 3,
        }

        public enum HistoryTicketStatus
        {
            UNPAID = 0,
            PAID = 1,
            CANCELLED = 2,
            USED = 3,
        }
        public enum RoleEnum
        {
            STUDENT ,
            PARENT ,
            OPERATOR,
            DRIVER,
            SCHOOL
        }
        public enum ShuttleSortByEnum
        {
            Name, // Sắp xếp theo tên xe
            Brand, // Sắp xếp theo hãng xe
            InsuranceExpiryDate
        }
    }
}
