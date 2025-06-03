using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ShuttleMate.Contract.Repositories.Base;

namespace ShuttleMate.Contract.Repositories.Entities
{
    public class DepartureTime : BaseEntity
    {
        public Guid RouteId { get; set; }
        public TimeOnly Departure { get; set; }
        public string DayOfWeek { get; set; }
        public virtual Route Route { get; set; }
        public virtual ICollection<StopEstimate> StopEstimates { get; set; } = new List<StopEstimate>();

    }
}
