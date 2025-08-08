using ShuttleMate.Contract.Repositories.Base;
using static ShuttleMate.Contract.Repositories.Enum.GeneralEnum;

namespace ShuttleMate.Contract.Repositories.Entities
{
    public class Schedule : BaseEntity
    {
        public DateOnly From { get; set; }
        public DateOnly To { get; set; }
        public RouteDirectionEnum Direction { get; set; }
        public TimeOnly DepartureTime { get; set; }
        public string DayOfWeek { get; set; }
        public Guid RouteId { get; set; }
        public virtual Route Route { get; set; }
        public Guid ShuttleId { get; set; }
        public virtual Shuttle Shuttle { get; set; }
        public Guid DriverId { get; set; }
        public virtual User Driver { get; set; }
        public Guid SchoolShiftId { get; set; }
        public virtual SchoolShift SchoolShift { get; set; }
        public virtual ICollection<ScheduleOverride> ScheduleOverrides { get; set; } = new List<ScheduleOverride>();
        public virtual ICollection<StopEstimate> StopEstimates { get; set; } = new List<StopEstimate>();
        public virtual ICollection<Trip> Trips { get; set; } = new List<Trip>();
    }
}
