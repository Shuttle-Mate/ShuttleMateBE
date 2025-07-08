using ShuttleMate.Contract.Repositories.Base;

namespace ShuttleMate.Contract.Repositories.Entities
{
    public class TicketPromotion : BaseEntity
    {
        public Guid PromotionId { get; set; }
        public Guid TicketId { get; set; }
        public virtual Promotion Promotion { get; set; }
        public virtual TicketType TicketType { get; set; }
    }
}
