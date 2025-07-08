using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ShuttleMate.Contract.Repositories.Base;

namespace ShuttleMate.Contract.Repositories.Entities
{
    public class RouteStop : BaseEntity
    {
        public Guid RouteId { get; set; }
        public Guid StopId { get; set; }
        public int StopOrder { get; set; }
        public virtual Route Route { get; set; }
        public virtual Stop Stop { get; set; }
    }
}
