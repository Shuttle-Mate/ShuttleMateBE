using ShuttleMate.Contract.Repositories.Base;
using static ShuttleMate.Contract.Repositories.Enum.GeneralEnum;

namespace ShuttleMate.Contract.Repositories.Entities
{
    public class HistoryTicket : BaseEntity
    {
        public HistoryTicketStatus Status { get; set; }
        public DateTime PurchaseAt { get; set; }
        public DateOnly ValidFrom { get; set; }
        public DateOnly ValidUntil { get; set; }
        public Guid TicketId { get; set; }
        public decimal Price { get; set; }
        public virtual Ticket Ticket { get; set; }
        public Guid UserId { get; set; }
        public virtual User User { get; set; }
        public virtual ICollection<Attendance> Attendances { get; set; } = new List<Attendance>();
        public virtual Transaction Transaction { get; set; }
    }
}
