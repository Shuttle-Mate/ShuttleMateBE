using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ShuttleMate.Contract.Repositories.Entities;

namespace ShuttleMate.ModelViews.RouteStopModelViews
{
    public class StopWithRouteResponseModel
    {
        public Guid StopId { get; set; }
        public string StopName { get; set; }
        public string Address { get; set; }
        public List<RouteResponseModel> Routes { get; set; } = new();
    }
}
