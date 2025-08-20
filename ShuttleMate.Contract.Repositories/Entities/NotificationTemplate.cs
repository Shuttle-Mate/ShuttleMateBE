using ShuttleMate.Contract.Repositories.Base;
using static ShuttleMate.Contract.Repositories.Enum.GeneralEnum;

namespace ShuttleMate.Contract.Repositories.Entities
{
    public class NotificationTemplate : BaseEntity
    {
        public string Type { get; set; }
        public string Name { get; set; }
        public string Template {  get; set; }
        public bool IsDefault { get; set; }
    }
}
