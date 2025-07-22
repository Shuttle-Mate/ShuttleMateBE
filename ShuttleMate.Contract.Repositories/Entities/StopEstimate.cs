using ShuttleMate.Contract.Repositories.Base;

namespace ShuttleMate.Contract.Repositories.Entities
{
    public class StopEstimate : BaseEntity
    {
        public TimeOnly ExpectedTime {  get; set; }
        public Guid StopId { get; set; }
        public virtual Stop Stop { get; set; }
        public Guid ScheduleId { get; set; }
        public virtual Schedule Schedule { get; set; }
    }
}
