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
        public virtual School School { get; set; }
        public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
        public virtual ICollection<Shuttle> Shuttles { get; set; } = new List<Shuttle>();
        public virtual ICollection<Feedback> Feedbacks { get; set; } = new List<Feedback>();
        public virtual ICollection<SupportRequest> SupportRequests { get; set; } = new List<SupportRequest>();
        public virtual ICollection<ChatBotLog> ChatBotLogs { get; set; } = new List<ChatBotLog>();
        public virtual ICollection<HistoryTicket> HistoryTickets { get; set; } = new List<HistoryTicket>();
        public virtual ICollection<NotificationRecipient> NotificationRecipients { get; set; } = new List<NotificationRecipient>();
        public virtual ICollection<SystemLogs> SystemLogs { get; set; } = new List<SystemLogs>();
    }
}
