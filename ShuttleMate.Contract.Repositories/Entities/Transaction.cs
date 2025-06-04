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
    public class Transaction : BaseEntity
    {
        public PaymentMethodEnum PaymentMethod { get; set; }
        public decimal Amount { get; set; }
        public virtual HistoryTicket HistoryTicket { get; set; }
    }
}
