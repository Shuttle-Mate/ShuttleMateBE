using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShuttleMate.ModelViews.TripModelViews
{
    public class TripHistoryModel
    {
        public Guid TripId { get; set; }
        public DateOnly TripDate { get; set; }
        public TimeOnly? StartTime { get; set; }
        public TimeOnly? EndTime { get; set; }
        public string RouteName { get; set; }
        public string RouteCode { get; set; }
        public string Shift { get; set; }
        public string Session { get; set; }
        public string DriverName { get; set; }
        public string ShuttleName { get; set; }
        public string Status { get; set; }
        public List<StudentAttendanceInfo> StudentAttendances { get; set; } // For driver
        public StudentAttendanceInfo MyAttendance { get; set; } // For student/parent
    }

    public class StudentAttendanceInfo
    {
        public Guid StudentId { get; set; }
        public string StudentName { get; set; }
        public string CheckInLocation { get; set; }
        public DateTime? CheckInTime { get; set; }
        public string CheckOutLocation { get; set; }
        public DateTime? CheckOutTime { get; set; }
        public string AttendanceStatus { get; set; }
    }
}
