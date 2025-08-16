using ShuttleMate.Contract.Repositories.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShuttleMate.Contract.Repositories.Entities
{
    public class HistoryTicketSchoolShift : BaseEntity
    {
        public Guid HistoryTicketId { get; set; }
        public Guid SchoolShiftId { get; set; }

        public virtual HistoryTicket HistoryTicket { get; set; }
        public virtual SchoolShift SchoolShift { get; set; }
    }
}
