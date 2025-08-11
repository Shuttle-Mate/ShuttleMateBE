using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static ShuttleMate.Contract.Repositories.Enum.GeneralEnum;

namespace ShuttleMate.ModelViews.SchoolModelView
{
    public class AttendanceOfSchoolResponseModel
    {
        public StudentResponse? Student { get; set; }
        public DateTime? CheckInTime { get; set; }
        public Guid? CheckInLocation { get; set; }
        public DateTime CheckOutTime { get; set; }
        public Guid? CheckOutLocation { get; set; }
        public string? AttendanceStatus { get; set; }
        public string? Notes { get; set; }
        public Guid? DriverId { get; set; }
        public string? DriverName { get; set; }
        public string? LicensePlate { get; set; }

    }
}
