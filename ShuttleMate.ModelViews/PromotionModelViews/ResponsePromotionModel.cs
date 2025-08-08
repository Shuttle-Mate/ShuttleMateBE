using static ShuttleMate.Contract.Repositories.Enum.GeneralEnum;

namespace ShuttleMate.ModelViews.PromotionModelViews
{
    public class ResponsePromotionModel
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public DateTime EndDate { get; set; }
        public string PromotionType { get; set; }
        public decimal? DiscountPrice { get; set; }
        public decimal? DiscountPercent { get; set; }
        public decimal? LimitSalePrice { get; set; }
        public int UsingLimit { get; set; }
        public int UsedCount { get; set; }
        public bool IsExpiredOrReachLimit { get; set; }
        public bool IsGlobal { get; set; }
        public string? ApplicableTicketType { get; set; }
        public Guid? TicketId { get; set; }
    }
}
