using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShuttleMate.ModelViews.SchoolModelView
{
    public class RouteToSchoolResponseModel
    {
        public string? RouteCode { get; set; }
        public string? RouteName { get; set; }
        public string? OperatingTime { get; set; }
        public decimal? Price { get; set; }
        public string? OutBound { get; set; }
        public string? InBound { get; set; }
        public decimal? TotalDistance { get; set; }
        public string? RunningTime { get; set; }
        public int? AmountOfTrip { get; set; }
        public string? Description { get; set; }
        public Guid SchoolId { get; set; }
        public string? SchoolName { get; set; }
    }
}
