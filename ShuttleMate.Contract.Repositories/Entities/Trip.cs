using ShuttleMate.Contract.Repositories.Base;
using static ShuttleMate.Contract.Repositories.Enum.GeneralEnum;

namespace ShuttleMate.Contract.Repositories.Entities
{
    public class Trip : BaseEntity
    {
        public DateOnly TripDate { get; set; }
        public TimeOnly? StartTime { get; set; }
        public TimeOnly? EndTime { get; set; }
        public TripStatusEnum Status { get; set; }
        public Guid ScheduleId { get; set; }
        public virtual Schedule Schedule { get; set; }
        public virtual ICollection<Attendance> Attendances { get; set; } = new List<Attendance>();
        public virtual ICollection<ShuttleLocationRecord> ShuttleLocationRecords { get; set; } = new List<ShuttleLocationRecord>();
        public virtual Feedback? Feedback { get; set; }
    }
}
