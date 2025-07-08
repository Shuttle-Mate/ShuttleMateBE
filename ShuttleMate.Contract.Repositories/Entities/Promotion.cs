using ShuttleMate.Contract.Repositories.Base;
using static ShuttleMate.Contract.Repositories.Enum.GeneralEnum;

namespace ShuttleMate.Contract.Repositories.Entities
{
    public class Promotion : BaseEntity
    {
        public string Description { get; set; }
        public decimal? DiscountPrice { get; set; } //DIRECT_DISCOUNT
        public decimal? DiscountPercent { get; set; } //PERCENTAGE_DISCOUNT
        public decimal? DiscountAmount { get; set; } //FIXED_AMOUNT_DISCOUNT
        public decimal? LimitSalePrice { get; set; }
        public DateTime EndDate { get; set; }
        public int UsingLimit { get; set; } = 0;
        public int UsedCount { get; set; } = 0;
        public bool IsExpiredOrReachLimit { get; set; } = false;
        public string Name { get; set; }
        public TypePromotionEnum Type { get; set; }
        public virtual ICollection<UserPromotion> UserPromotions { get; set; } = new List<UserPromotion>();
        public virtual ICollection<TicketPromotion> TicketPromotions { get; set; } = new List<TicketPromotion>();
    }
}
