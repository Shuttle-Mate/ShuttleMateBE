using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.Pkcs;
using System.Text;
using System.Threading.Tasks;
using ShuttleMate.Contract.Repositories.Base;
using ShuttleMate.Core.Utils;
using static ShuttleMate.Contract.Repositories.Enum.GeneralEnum;

namespace ShuttleMate.Contract.Repositories.Entities
{
    public class Promotion : BaseEntity
    {
        public string Description { get; set; }
        public decimal DiscountPrice { get; set; }
        public decimal DiscountPercent { get; set; }
        public decimal LimitSalePrice { get; set; }
        public DateTime EndDate { get; set; }
        public int UsingLimit {  get; set; }
        public int UsedCount { get; set; }
        public bool IsExpiredOrReachLimit { get; set; }
        public string Name { get; set; }
        public TypePromotionEnum Type { get; set; }
        public string UserId { get; set; }
        public virtual ICollection<TicketPromotion> TicketPromotions { get; set; } = new List<TicketPromotion>();
    }
}
