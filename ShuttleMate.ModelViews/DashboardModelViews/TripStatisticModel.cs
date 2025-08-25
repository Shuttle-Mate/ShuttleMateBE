using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShuttleMate.ModelViews.DashboardModelViews
{
    public class TripStatisticModel
    {
        public int TotalTripToday { get; set; }
        public int TotalTripThisWeek { get; set; }
        public int TotalTripThisMonth { get; set; }
        public List<TripChartData> TripChart { get; set; } = new();
    }

    public class TripChartData
    {
        public DateTime Date { get; set; }
        public int TripCount { get; set; }
    }
}
