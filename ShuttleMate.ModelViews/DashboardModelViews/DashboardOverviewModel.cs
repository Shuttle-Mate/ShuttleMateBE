using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShuttleMate.ModelViews.DashboardModelViews
{
    public class DashboardOverviewModel
    {
        public int TotalUser { get; set; }
        public int TotalStudent { get; set; }
        public int TotalDriver { get; set; }
        public int TotalTrip { get; set; }
        public int TotalShuttle { get; set; }
        public int TotalSchool { get; set; }
        public decimal TotalRevenue { get; set; }
        public int TotalTransaction { get; set; }
    }
}
