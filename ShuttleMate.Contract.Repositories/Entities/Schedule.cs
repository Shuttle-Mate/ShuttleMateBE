using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ShuttleMate.Contract.Repositories.Base;
using ShuttleMate.Core.Utils;

namespace ShuttleMate.Contract.Repositories.Entities
{
    public class Schedule : BaseEntity
    {
        public TimeOnly DepartureTime { get; set; }
        public string DayOfWeek { get; set; }
        public string RouteId { get; set; }
        public string ShuttleId { get; set; }
        public virtual Route Route { get; set; }
        public virtual Shuttle Shuttle { get; set; }
        public virtual ICollection<TicketSchedule> TicketSchedules { get; set; } = new List<TicketSchedule>();

        public Schedule()
        {
            CreatedTime = CoreHelper.SystemTimeNow;
            LastUpdatedTime = CreatedTime;
        }
    }
}
