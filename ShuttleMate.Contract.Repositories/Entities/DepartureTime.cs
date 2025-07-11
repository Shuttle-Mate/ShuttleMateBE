using ShuttleMate.Contract.Repositories.Base;

namespace ShuttleMate.Contract.Repositories.Entities
{
    public class DepartureTime : BaseEntity
    {
        public Guid RouteId { get; set; }
        public TimeOnly Time { get; set; }
        public string DayOfWeek { get; set; }
        public virtual Route Route { get; set; }
        public virtual ICollection<StopEstimate> StopEstimates { get; set; } = new List<StopEstimate>();
    }
}
