using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ShuttleMate.Contract.Repositories.Base;
using ShuttleMate.Core.Utils;

namespace ShuttleMate.Contract.Repositories.Entities
{
    public class NotificationRecipient : BaseEntity
    {
        public string RecipientId { get; set; }
        public string NotificationId { get; set; }
        public string RecipientType { get; set; }
        //public enum Status { get; set; }
        public virtual User Recipient { get; set; }
        public virtual Notification Notification { get; set; }
        public NotificationRecipient()
        {
            CreatedTime = CoreHelper.SystemTimeNow;
            LastUpdatedTime = CreatedTime;
        }
    }
}
