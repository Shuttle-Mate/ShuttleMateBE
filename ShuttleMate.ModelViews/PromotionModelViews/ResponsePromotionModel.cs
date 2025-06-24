namespace ShuttleMate.ModelViews.PromotionModelViews
{
    public class ResponsePromotionModel
    {
        public Guid Id { get; set; }
        public string Description { get; set; }
        public decimal DiscountPrice { get; set; }
        public decimal DiscountPercent { get; set; }
        public decimal LimitSalePrice { get; set; }
        public DateTime EndDate { get; set; }
        public int UsingLimit { get; set; }
        public int UsedCount { get; set; }
        public bool IsExpiredOrReachLimit { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
        public Guid UserId { get; set; }
    }
}
