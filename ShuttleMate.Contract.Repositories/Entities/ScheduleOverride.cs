using ShuttleMate.Contract.Repositories.Base;

namespace ShuttleMate.Contract.Repositories.Entities
{
    public class ScheduleOverride : BaseEntity
    {
        public DateOnly Date { get; set; }
        public string? Reason { get; set; }
        public Guid ScheduleId { get; set; }
        public virtual Schedule Schedule { get; set; }
        public Guid ShuttleId { get; set; }
        public virtual Shuttle Shuttle { get; set; }
        public Guid? OriginalUserId { get; set; }
        public virtual User? OriginalUser { get; set; }
        public Guid OverrideUserId { get; set; }
        public virtual User OverrideUser { get; set; }
    }
}
