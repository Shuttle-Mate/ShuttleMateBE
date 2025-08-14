using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShuttleMate.ModelViews.UserModelViews
{
    public class GetAttendanceForUserModel
    {
        public Guid? Id { get; set; }
        public string? ShiftType { get; set; }
        public string? SessionType { get; set; }
        public DateTime? CheckInTime { get; set; }
        public string? CheckOutLocation { get; set; }
        public TimeOnly? Time { get; set; }
        public string? CheckInLocation { get; set; }
        public DateTime? CheckOutTime { get; set; }

    }
}
