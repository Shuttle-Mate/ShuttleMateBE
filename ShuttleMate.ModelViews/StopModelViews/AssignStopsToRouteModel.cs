using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShuttleMate.ModelViews.StopModelViews
{
    public class AssignStopsToRouteModel
    {
        public Guid RouteId { get; set; }
        public List<Guid> StopIds { get; set; } = new();
    }
}
