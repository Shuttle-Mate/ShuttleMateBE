using ShuttleMate.Contract.Repositories.Base;

namespace ShuttleMate.Contract.Repositories.Entities
{
    public class ScheduleOverride : BaseEntity
    {
        public DateOnly Date { get; set; }
        public string? Reason { get; set; }
        public Guid ScheduleId { get; set; }
        public virtual Schedule Schedule { get; set; }
        public Guid OriginalShuttleId { get; set; }
        public virtual Shuttle OriginalShuttle { get; set; }
        public Guid? OverrideShuttleId { get; set; }
        public virtual Shuttle? OverrideShuttle { get; set; }
        public Guid OriginalUserId { get; set; }
        public virtual User OriginalUser { get; set; }
        public Guid? OverrideUserId { get; set; }
        public virtual User? OverrideUser { get; set; }
    }
}
