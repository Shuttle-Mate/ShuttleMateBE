namespace ShuttleMate.ModelViews.PromotionModelViews
{
    public class CreatePromotionModel
    {
        public string TicketType { get; set; }
        public string PromotionType { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public decimal DiscountValue { get; set; }
        public decimal? LimitSalePrice { get; set; }
        public DateTime EndDate { get; set; }
        public int UsingLimit { get; set; }
    }
}
