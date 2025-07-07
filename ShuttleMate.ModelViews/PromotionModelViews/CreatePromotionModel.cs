namespace ShuttleMate.ModelViews.PromotionModelViews
{
    public class CreatePromotionModel
    {
        public string Type { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public decimal DiscountValue { get; set; }
        public decimal? LimitSalePrice { get; set; }
        public DateTime EndDate { get; set; }
        public int UsingLimit { get; set; }
        public Guid TicketTypeId { get; set; }
    }
}
