using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShuttleMate.ModelViews.DashboardModelViews
{
    public class AttendanceStatisticsModel
    {
        public int TotalCheckInToday { get; set; }
        public int TotalCheckOutToday { get; set; }
        public int TotalAbsentToday { get; set; }
        public List<AttendanceChartData> AttendanceChart { get; set; } = new();
    }

    public class AttendanceChartData
    {
        public DateTime Date { get; set; }
        public int CheckInCount { get; set; }
        public int CheckOutCount { get; set; }
        public int AbsentCount { get; set; }
    }
}
