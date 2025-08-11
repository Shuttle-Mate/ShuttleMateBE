using ShuttleMate.Contract.Repositories.Base;
using static ShuttleMate.Contract.Repositories.Enum.GeneralEnum;

namespace ShuttleMate.Contract.Repositories.Entities
{
    public class NotificationRecipient : BaseEntity
    {
        public NotificationCategoryEnum NotificationCategory { get; set; }
        public string RecipientType { get; set; }
        public NotificationStatusEnum Status { get; set; }
        public Guid NotificationId { get; set; }
        public virtual Notification Notification { get; set; }
        public Guid RecipientId { get; set; }
        public virtual User Recipient { get; set; }
    }
}
