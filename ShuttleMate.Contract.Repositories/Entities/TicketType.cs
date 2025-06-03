using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ShuttleMate.Contract.Repositories.Base;
using ShuttleMate.Core.Utils;

namespace ShuttleMate.Contract.Repositories.Entities
{
    public class TicketType : BaseEntity
    {
        //public enum ticket_type { get; set; }
        public decimal Price { get; set; }
        public virtual ICollection<TicketPromotion> TicketPromotions { get; set; } = new List<TicketPromotion>();
        public virtual ICollection<TicketSchedule> TicketSchedules { get; set; } = new List<TicketSchedule>();
        public virtual ICollection<HistoryTicket> HistoryTickets { get; set; } = new List<HistoryTicket>();
    }
}
