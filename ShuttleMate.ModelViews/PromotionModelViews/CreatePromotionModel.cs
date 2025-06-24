using static ShuttleMate.Contract.Repositories.Enum.GeneralEnum;

namespace ShuttleMate.ModelViews.PromotionModelViews
{
    public class CreatePromotionModel
    {
        public string Description { get; set; }
        public decimal DiscountPrice { get; set; }
        public decimal DiscountPercent { get; set; }
        public decimal LimitSalePrice { get; set; }
        public DateTime EndDate { get; set; }
        public int UsingLimit { get; set; }
        public int UsedCount { get; set; }
        public bool IsExpiredOrReachLimit { get; set; }
        public string Name { get; set; }
        public TypePromotionEnum Type { get; set; }
        public Guid UserId { get; set; }
    }
}
