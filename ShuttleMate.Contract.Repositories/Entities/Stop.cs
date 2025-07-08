using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ShuttleMate.Contract.Repositories.Base;

namespace ShuttleMate.Contract.Repositories.Entities
{
    public class Stop : BaseEntity
    {
        public string Name { get; set; }
        public string Ward {  get; set; }
        public decimal Lat {  get; set; }
        public decimal Lng { get; set; }
        public virtual ICollection<StopEstimate> StopEstimates { get; set; } = new List<StopEstimate>();
        public virtual ICollection<RouteStop> RouteStops { get; set; } = new List<RouteStop>();
    }
}
