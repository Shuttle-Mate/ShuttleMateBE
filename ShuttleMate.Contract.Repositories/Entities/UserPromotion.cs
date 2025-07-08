using ShuttleMate.Contract.Repositories.Base;

namespace ShuttleMate.Contract.Repositories.Entities
{
    public class UserPromotion : BaseEntity
    {
        public Guid UserId { get; set; }
        public Guid PromotionId { get; set; }
        public bool IsUsed { get; set; } = false;
        public virtual User User { get; set; }
        public virtual Promotion Promotion { get; set; }
    }
}
