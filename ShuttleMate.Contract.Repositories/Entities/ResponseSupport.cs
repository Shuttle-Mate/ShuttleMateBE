using ShuttleMate.Contract.Repositories.Base;

namespace ShuttleMate.Contract.Repositories.Entities
{
    public class ResponseSupport : BaseEntity
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public Guid SupportRequestId { get; set; }
        public virtual SupportRequest SupportRequest { get; set; }
    }
}
