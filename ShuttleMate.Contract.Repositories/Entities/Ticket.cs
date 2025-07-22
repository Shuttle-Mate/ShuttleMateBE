using ShuttleMate.Contract.Repositories.Base;
using static ShuttleMate.Contract.Repositories.Enum.GeneralEnum;

namespace ShuttleMate.Contract.Repositories.Entities
{
    public class Ticket : BaseEntity
    {
        public TicketTypeEnum Type { get; set; }
        public decimal Price { get; set; }
        public Guid RouteId { get; set; }
        public virtual Route Route { get; set; }
        public virtual ICollection<HistoryTicket> HistoryTickets { get; set; } = new List<HistoryTicket>();
        public virtual ICollection<Promotion> Promotions { get; set; } = new List<Promotion>();
    }
}
