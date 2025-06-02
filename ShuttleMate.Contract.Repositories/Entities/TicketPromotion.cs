using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ShuttleMate.Contract.Repositories.Base;
using ShuttleMate.Core.Utils;

namespace ShuttleMate.Contract.Repositories.Entities
{
    public class TicketPromotion : BaseEntity
    {
        public string PromotionId { get; set; }
        public string TicketId { get; set; }
        public virtual Promotion Promotion { get; set; }
        public virtual TicketType TicketType { get; set; }
        public TicketPromotion()
        {
            CreatedTime = CoreHelper.SystemTimeNow;
            LastUpdatedTime = CreatedTime;
        }
    }
}
