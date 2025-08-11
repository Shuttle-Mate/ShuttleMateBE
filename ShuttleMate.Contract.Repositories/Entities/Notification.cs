using ShuttleMate.Contract.Repositories.Base;
using static ShuttleMate.Contract.Repositories.Enum.GeneralEnum;

namespace ShuttleMate.Contract.Repositories.Entities
{
    public class Notification : BaseEntity
    {
        public NotificationCategoryEnum NotificationCategory { get; set; }
        public string Title { get; set; }
        public string Content { get; set; }
        public string TemplateType { get; set; }
        public NotificationStatusEnum Status { get; set; }
        public virtual ICollection<NotificationRecipient> NotificationRecipients { get; set; } = new List<NotificationRecipient>();
    }
}
