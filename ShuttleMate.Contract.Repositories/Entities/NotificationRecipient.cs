using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ShuttleMate.Contract.Repositories.Base;
using ShuttleMate.Core.Utils;
using static ShuttleMate.Contract.Repositories.Enum.GeneralEnum;

namespace ShuttleMate.Contract.Repositories.Entities
{
    public class NotificationRecipient : BaseEntity
    {
        public Guid RecipientId { get; set; }
        public Guid NotificationId { get; set; }
        public string RecipientType { get; set; }
        public NotificationStatusEnum Status { get; set; }
        public virtual User Recipient { get; set; }
        public virtual Notification Notification { get; set; }

    }
}
