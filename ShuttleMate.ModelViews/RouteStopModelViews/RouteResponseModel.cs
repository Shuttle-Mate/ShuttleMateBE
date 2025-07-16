using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShuttleMate.ModelViews.RouteStopModelViews
{
    public class RouteResponseModel
    {
        public Guid RouteId { get; set; }
        public string RouteCode { get; set; }
        public string RouteName { get; set; }
    }
}
