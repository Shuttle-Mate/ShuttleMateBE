using ShuttleMate.Contract.Repositories.Base;
using static ShuttleMate.Contract.Repositories.Enum.GeneralEnum;

namespace ShuttleMate.Contract.Repositories.Entities
{
    public class SupportRequest : BaseEntity 
    {
        public SupportRequestCategoryEnum Category { get; set; }
        public SupportRequestStatusEnum Status { get; set; }
        public string Title { get; set; }
        public string Message { get; set; }
        public Guid UserId { get; set; }
        public virtual User User { get; set; }
        public virtual ICollection<ResponseSupport> ResponseSupports { get; set; } = new List<ResponseSupport>();

    }
}
