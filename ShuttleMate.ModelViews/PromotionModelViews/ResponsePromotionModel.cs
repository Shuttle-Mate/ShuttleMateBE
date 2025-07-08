namespace ShuttleMate.ModelViews.PromotionModelViews
{
    public class ResponsePromotionModel
    {
        public Guid Id { get; set; }
        public string Type { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public decimal? DiscountPrice { get; set; }
        public decimal? DiscountPercent { get; set; }
        public decimal? DiscountAmount { get; set; }
        public decimal? LimitSalePrice { get; set; }
        public DateTime EndDate { get; set; }
        public int UsingLimit { get; set; }
        public int UsedCount { get; set; }
        public bool IsExpiredOrReachLimit { get; set; }
        public List<ResponseTicketPromotionModel> TicketTypes { get; set; } = new();
    }

    public class ResponseTicketPromotionModel
    {
        public Guid TicketId { get; set; }
        public string Type { get; set; }
    }
}
