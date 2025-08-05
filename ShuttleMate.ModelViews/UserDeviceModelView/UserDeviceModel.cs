using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShuttleMate.ModelViews.UserDeviceModelView
{
    public class UserDeviceModel
    {
        public string? DeviceType { get; set; }
        public string? DeviceName { get; set; }
        public string? OsVersion { get; set; }
        public string PushToken { get; set; }
    }
}
