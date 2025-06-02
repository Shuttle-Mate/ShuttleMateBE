using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ShuttleMate.Contract.Repositories.Base;
using ShuttleMate.Core.Utils;

namespace ShuttleMate.Contract.Repositories.Entities
{
    public class Transaction : BaseEntity
    {
        //public enum PaymentMethod { get; set; }
        public decimal Amount { get; set; }
        public virtual ICollection<HistoryTicket> HistoryTickets { get; set; } = new List<HistoryTicket>();

        public Transaction()
        {
            CreatedTime = CoreHelper.SystemTimeNow;
            LastUpdatedTime = CreatedTime;
        }
    }
}
