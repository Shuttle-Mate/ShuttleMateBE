using ShuttleMate.Contract.Repositories.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static ShuttleMate.Contract.Repositories.Enum.GeneralEnum;

namespace ShuttleMate.Contract.Repositories.Entities
{
    public class UserDevice : BaseEntity
    {
        public string? DeviceType {  get; set; }
        public string? DeviceName { get; set; }
        public string? OsVersion { get; set; }
        public string PushToken { get; set; }
        public bool IsValid { get; set; }
        public Guid UserId { get; set; }
        public virtual User User { get; set; }
    }
}
