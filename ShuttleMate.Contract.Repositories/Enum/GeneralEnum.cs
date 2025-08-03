namespace ShuttleMate.Contract.Repositories.Enum
{
    public class GeneralEnum
    {
        public enum RouteDirectionEnum
        {
            IN_BOUND,
            OUT_BOUND
        }

        public enum TripStatusEnum
        {
            SCHEDULED, //Scheduled but not start yet
            IN_PROGESS,
            COMPLETED,
            CANCELLED
        }

        public enum AttendanceStatusEnum
        {
            NOT_CHECKED_IN,
            CHECKED_IN,
            CHECKED_OUT
        }

        public enum ChatBotRoleEnum
        {
            USER,
            MODEL
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
            PENDING,         //Đã tạo nhưng chưa gửi
            SENT,            //Đã gửi thành công (đẩy qua push, email, v.v.)
            DELIVERED,       //Đã nhận ở thiết bị người dùng (nếu có tracking)
            READ,            //Người dùng đã mở và đọc thông báo
            FAILED,          //Gửi thất bại (VD: push token hết hạn)
            ARCHIEVED,        //Đã lưu trữ, không hiển thị trên UI
            CANCELLED         //Đã hủy (VD: hủy gửi trước giờ chạy)
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
            IN_PROGRESS,         // Đang xử lí
            COMPLETED,           // Đã hoàn thành
            REJECTED,            // Bị từ chối
        }

        public enum TicketTypeEnum
        {
            //SINGLE_RIDE,
            DAY_PASS,
            WEEKLY,
            MONTHLY,
            SEMESTER_ONE,
            SEMESTER_TWO
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
            NAME, // Sắp xếp theo tên xe
            BRAND, // Sắp xếp theo hãng xe
            INSURANCE_EXPIRY_DATE, // Sắp xếp theo ngày hết hạn bảo hiểm
        }

        public enum ShiftTypeEnum
        {
            START, // Giờ vào
            END    // Giờ tan
        }

        public enum SessionTypeEnum
        {
            MORNING, // Sáng
            AFTERNOON // Chiều
        }
    }
}
