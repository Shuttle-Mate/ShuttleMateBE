using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ShuttleMate.Contract.Repositories.Base;
using ShuttleMate.Core.Utils;

namespace ShuttleMate.Contract.Repositories.Entities
{
    public class TicketSchedule : BaseEntity
    {
        public string ScheduleId { get; set; }
        public string TicketId { get; set; }
        public virtual Schedule Schedule { get; set; }
        public virtual TicketType TicketType { get; set; }
    }
}
