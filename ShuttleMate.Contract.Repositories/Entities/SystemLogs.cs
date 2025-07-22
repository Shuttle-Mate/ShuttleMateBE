using ShuttleMate.Contract.Repositories.Base;

namespace ShuttleMate.Contract.Repositories.Entities
{
    public class SystemLogs : BaseEntity
    {
        public string Action { get; set; }
        public Guid ActorId { get; set; }
        public virtual User Actor { get; set; }
    }
}
