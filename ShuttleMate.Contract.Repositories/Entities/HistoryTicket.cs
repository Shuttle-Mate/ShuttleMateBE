using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;
using ShuttleMate.Contract.Repositories.Base;
using ShuttleMate.Core.Utils;
using static ShuttleMate.Contract.Repositories.Enum.GeneralEnum;

namespace ShuttleMate.Contract.Repositories.Entities
{
    public class HistoryTicket : BaseEntity
    {
        public Guid UserId { get; set; }
        public Guid TicketId { get; set; }
        public DateTime PurchaseAt { get; set; }
        public DateTime ValidFrom { get; set; }
        public DateTime ValidUntil { get; set; }
        public virtual User User { get; set; }
        public virtual TicketType TicketType { get; set; }
        public HistoryTicketStatus Status { get; set; }
        public virtual ICollection<Attendance> Attendances { get; set; } = new List<Attendance>();

        // Navigation property từ HistoryTicket đến Transaction
        public virtual Transaction Transaction { get; set; } // Không cần thêm HistoryTicketId ở đây
    }

}
