using Microsoft.AspNetCore.Identity;
using ShuttleMate.Contract.Repositories.Base;
using ShuttleMate.Core.Utils;

namespace ShuttleMate.Contract.Repositories.Entities
{
    public class User : BaseEntity
    {
        public string FullName { get; set; } = string.Empty;
        public bool Gender { get; set; } = true;
        public DateTime? DateOfBirth { get; set; }
        public string? ProfileImageUrl { get; set; }
        public string? Address { get; set; }
        public string? IdentificationNumber { get; set; }
        public int? EmailCode { get; set; }
        public string Email {  get; set; } 
        public string UserName { get; set; }
        public string PasswordHash { get; set; }
        public string PhoneNumber { get; set; }
        public bool? EmailVerified { get; set; }
        public string? RefeshToken { get; set; }
        public DateTime? CodeGeneratedTime { get; set; }
        public bool? Violate { get; set; } = false;
        public Guid? SchoolId { get; set; }
        public TimeOnly? SchoolTime { get; set; }
        public Guid? ParentId { get; set; }
        public virtual User Parent { get; set; }
        public virtual User School { get; set; }
        public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
        public virtual ICollection<Shuttle> Shuttles { get; set; } = new List<Shuttle>();
        public virtual ICollection<Feedback> Feedbacks { get; set; } = new List<Feedback>();
        public virtual ICollection<SupportRequest> SupportRequests { get; set; } = new List<SupportRequest>();
        public virtual ICollection<ChatBotLog> ChatBotLogs { get; set; } = new List<ChatBotLog>();
        public virtual ICollection<HistoryTicket> HistoryTickets { get; set; } = new List<HistoryTicket>();
        public virtual ICollection<NotificationRecipient> NotificationRecipients { get; set; } = new List<NotificationRecipient>();
        public virtual ICollection<SystemLogs> SystemLogs { get; set; } = new List<SystemLogs>();
        public virtual ICollection<UserPromotion> UserPromotions { get; set; } = new List<UserPromotion>();
        public virtual ICollection<Route> Routes { get; set; } = new List<Route>();
        public virtual ICollection<User> Students { get; set; } = new List<User>();
        public virtual ICollection<DepartureTime> DepartureTimes { get; set; } = new List<DepartureTime>();
        // 1. Các bản ghi override mà người dùng là người bị thay thế
        public virtual ICollection<ScheduleOverride> OriginalScheduleOverrides { get; set; } = new List<ScheduleOverride>();
        // 2. Các bản ghi override mà người dùng là người được gắn vào (thay thế)
        public virtual ICollection<ScheduleOverride> OverrideScheduleOverrides { get; set; } = new List<ScheduleOverride>();
        // 3. Các bản ghi override do người dùng tạo
        public virtual ICollection<ScheduleOverride> CreatedScheduleOverrides { get; set; } = new List<ScheduleOverride>();
    }
}
