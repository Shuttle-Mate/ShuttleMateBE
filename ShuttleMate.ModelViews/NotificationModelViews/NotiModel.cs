using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static ShuttleMate.Contract.Repositories.Enum.GeneralEnum;

namespace ShuttleMate.ModelViews.NotificationModelViews
{
    public class NotiModel
    {
        public string Title { get; set; }
        public string Content { get; set; }
        public string TemplateType { get; set; }
        public string NotificationCategory { get; set; }
        //public NotificationStatusEnum Status { get; set; }
    }
}
