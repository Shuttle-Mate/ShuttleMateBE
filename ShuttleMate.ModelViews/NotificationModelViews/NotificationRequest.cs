using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static ShuttleMate.Contract.Repositories.Enum.GeneralEnum;

namespace ShuttleMate.ModelViews.NotificationModelViews
{
    public class NotificationRequest
    {
        public string Title { get; set; }
        public string Body { get; set; }
        public string DeviceToken { get; set; }
    }

    public class NotificationTemplateSendRequest
    {
        public string TemplateType { get; set; } = default!;
        public List<Guid> RecipientIds { get; set; } = new();
        public Dictionary<string, string> Metadata { get; set; } = new();
        public string CreatedBy { get; set; } = "System";
        public string NotificationCategory { get; set; }
    }

    public class NotificationTemplateSendAllRequest
    {
        public string TemplateType { get; set; } = default!;
        public Dictionary<string, string> Metadata { get; set; } = new();
        public string NotificationCategory { get; set; }
    }

}
