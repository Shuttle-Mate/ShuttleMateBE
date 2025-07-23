using ShuttleMate.Contract.Repositories.Base;
using static ShuttleMate.Contract.Repositories.Enum.GeneralEnum;

namespace ShuttleMate.Contract.Repositories.Entities
{
    public class Feedback : BaseEntity
    {
        public FeedbackCategoryEnum FeedbackCategory { get; set; }
        public string Message { get; set; }
        public int Rating { get; set; }
        public Guid TripId { get; set; }
        public virtual Trip Trip { get; set; }
        public Guid UserId { get; set; }
        public virtual User User { get; set; }
    }
}
