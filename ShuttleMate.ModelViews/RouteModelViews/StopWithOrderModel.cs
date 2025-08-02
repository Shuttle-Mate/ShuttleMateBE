using ShuttleMate.ModelViews.StopModelViews;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShuttleMate.ModelViews.RouteModelViews
{
    public class StopWithOrderModel
    {
        public BasicStopModel Stop {  get; set; }
        public int StopOrder { get; set; }
        public int Duration { get; set; }
    }
}
