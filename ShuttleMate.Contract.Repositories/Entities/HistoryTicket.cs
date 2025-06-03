using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;
using ShuttleMate.Contract.Repositories.Base;
using ShuttleMate.Core.Utils;

namespace ShuttleMate.Contract.Repositories.Entities
{
    public class HistoryTicket : BaseEntity
    {
        public string UserId { get; set; }
        public string TransactionId { get; set; }
        public string TicketId { get; set; }
        //public enum TicketStatus { get; set; }
        //public enum TripStatus { get; set; }
        public DateTime PurchaseAt { get; set; }
        public DateTime ValidFrom { get; set; }
        public DateTime ValidUntil { get; set; }
        public virtual User User { get; set; }
        public virtual Transaction Transaction { get; set; }
        public virtual TicketType TicketType { get; set; }

    }
}
