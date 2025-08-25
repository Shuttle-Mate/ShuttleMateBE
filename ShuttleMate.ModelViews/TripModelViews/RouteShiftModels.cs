using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShuttleMate.ModelViews.TripModelViews
{
    public class RouteShiftModels
    {
        public Guid RouteID {  get; set; }
        public List<Guid>? SchoolShiftId { get; set; }
    } 
}
