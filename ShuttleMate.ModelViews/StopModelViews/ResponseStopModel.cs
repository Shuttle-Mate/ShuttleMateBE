using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShuttleMate.ModelViews.StopModelViews
{
    public class ResponseStopModel
    {
        public Guid Id { get; set; }
        public Guid RouteId { get; set; }
        public string Name { get; set; }
        public string Ward { get; set; }
        public decimal Lat { get; set; }
        public decimal Lng { get; set; }
        public int StopOrder { get; set; }
    }
}
