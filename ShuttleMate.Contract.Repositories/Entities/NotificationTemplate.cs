using ShuttleMate.Contract.Repositories.Base;

namespace ShuttleMate.Contract.Repositories.Entities
{
    public class NotificationTemplate : BaseEntity
    {
        public string Type { get; set; }
        public string Template {  get; set; }
    }
}
