using ShuttleMate.Contract.Repositories.Base;

namespace ShuttleMate.Contract.Repositories.Entities
{
    public class DepartureTime : BaseEntity
    {
        public Guid RouteId { get; set; }
        public TimeOnly Time { get; set; }
        public string DayOfWeek { get; set; }
        public Guid ShuttleId { get; set; }
        public Guid OperatorId { get; set; }
        public virtual Shuttle Shuttle { get; set; }
        public virtual User User { get; set; }
        public virtual Route Route { get; set; }
        public virtual ICollection<StopEstimate> StopEstimates { get; set; } = new List<StopEstimate>();
    }
}
