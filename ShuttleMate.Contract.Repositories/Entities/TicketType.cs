using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ShuttleMate.Contract.Repositories.Base;
using ShuttleMate.Core.Utils;
using static ShuttleMate.Contract.Repositories.Enum.GeneralEnum;

namespace ShuttleMate.Contract.Repositories.Entities
{
    public class TicketType : BaseEntity
    {
        public Guid RouteId { get; set; }
        public TicketTypeEnum Type { get; set; }
        public decimal Price { get; set; }
        public virtual Route Route { get; set; }
        public virtual ICollection<TicketPromotion> TicketPromotions { get; set; } = new List<TicketPromotion>();
        public virtual ICollection<HistoryTicket> HistoryTickets { get; set; } = new List<HistoryTicket>();
    }
}
