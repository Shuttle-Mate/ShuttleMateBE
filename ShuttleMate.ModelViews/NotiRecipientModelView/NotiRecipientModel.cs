using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static ShuttleMate.Contract.Repositories.Enum.GeneralEnum;

namespace ShuttleMate.ModelViews.NotiRecipientModelView
{
    public class NotiRecipientModel
    {
        public Guid RecipientId { get; set; }
        public Guid NotificationId { get; set; }
        public string RecipientType { get; set; }
        //public NotificationStatusEnum Status { get; set; }
    }
}
