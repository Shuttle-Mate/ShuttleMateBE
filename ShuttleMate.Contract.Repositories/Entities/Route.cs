using ShuttleMate.Contract.Repositories.Base;

namespace ShuttleMate.Contract.Repositories.Entities
{
    public class Route : BaseEntity
    {
        public string RouteCode { get; set; }
        public string RouteName { get; set; }
        public string Description { get; set; }
        public string? OperatingTime { get; set; }
        public decimal? Price { get; set; }
        public string? OutBound { get; set; }
        public string? InBound { get; set; }
        public decimal? TotalDistance { get; set; }
        public string? RunningTime { get; set; }
        public int AmountOfTrip { get; set; }
        public bool IsActive { get; set; }
        public Guid SchoolId { get; set; }
        public virtual School School { get; set; }
        public virtual ICollection<RouteStop> RouteStops { get; set; } = new List<RouteStop>();
        public virtual ICollection<Schedule> Schedules { get; set; } = new List<Schedule>();
        public virtual ICollection<ScheduleOverride> ScheduleOverrides { get; set; } = new List<ScheduleOverride>();
        public virtual ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
    }
}
